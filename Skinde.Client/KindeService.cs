using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using Skinde.Client.Kinde;
using Skinde.Utility;

namespace Skinde.Client;

public class KindeService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(Constants.ApiClient.Kinde);
    private readonly string _clientId = configuration.Setting(Utility.Constants.SettingName.KindeApiClientId);
    private readonly string _clientSecret = configuration.Setting(Utility.Constants.SettingName.KindeApiClientSecret, true);

    private string _token = string.Empty;
    private long _tokenExpiration;

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Organization-Based Services
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public async Task<Organization> GetOrganization(string orgCode) => await ApiCall<Organization>(HttpMethod.Get, $"organization", null, ("code", orgCode));

    public async Task<List<Organization>> GetOrganizations() => await GetEntities<Organization, OrganizationsResponse>("organizations", nameof(OrganizationsResponse.Organizations));

    public async Task<Response> AddOrganization(Organization organization) => await ApiCall<Response, Organization>(HttpMethod.Post, "organization", null, organization);

    public async Task<Response> UpdateOrganization(Organization organization)
    {
        if (string.IsNullOrEmpty(organization.Code)) throw new ArgumentException($"{nameof(Organization)} {nameof(Organization.Code)} is required for updating.");
        return await ApiCall<Response, Organization>(HttpMethod.Patch, $"organization/{organization.Code}", null, organization);
    }

    public async Task<Response> AddOrganizationUsers(string orgCode, OrganizationUsersRequest orgUsersReq) => await ApiCall<Response, OrganizationUsersRequest>(HttpMethod.Post, $"organizations/{orgCode}/users", null, orgUsersReq);

    public async Task<Response> RemoveOrganizationUser(string orgCode, string userId) => await ApiCall<Response>(HttpMethod.Delete, $"organizations/{orgCode}/users/{userId}");

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Role-Based Services
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    
    public async Task<List<Role>> GetRoles() => await GetEntities<Role, RolesResponse>("roles", nameof(RolesResponse.Roles));

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // User-Based Services
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    //public async Task<List<User>> GetUsers() => await GetEntities<User, UsersResponse>("users", nameof(UsersResponse.Users), ("expand", "organizations"));
    public async Task<List<User>> GetUsers(bool includeRoleId = false)
    {
        var users = await GetEntities<User, UsersResponse>("users", nameof(UsersResponse.Users), ("expand", "organizations"));
        if (!includeRoleId) return users;

        foreach (var user in users)
        {
            if (user.Id == null) continue;

            foreach (var orgCode in user.Organizations)
            {
                if (user?.Id == null) continue;

                var capturedUser = user;
                var capturedOrgCode = orgCode;

                var task = Task.Run(async () =>
                {
                    // Get roles for the user in the organization
                    var roles = await GetUserRoles(capturedUser.Id, capturedOrgCode);
                    if (roles.Count == 0) return;

                    lock (capturedUser.Roles)
                    {
                        capturedUser.Roles.AddRange(roles.Select(r => r.Id!));
                    }
                });
            }
        }

        return users;
    }

    public async Task<User> GetUser(string userId)
    {
        var user = await ApiCall<User>(HttpMethod.Get, $"user", null, ("id", userId), ("expand", "organizations"));

        var identities = await GetUserIdentities(userId);
        var phoneIdentity = identities.FirstOrDefault(i => i.Type == "phone");
        if (phoneIdentity != null) user.Phone = phoneIdentity.Value;

        return user;
    }

    public async Task<UserCreateResponse> AddUser(User user)
    {
        var userCreateRequest = new UserCreateRequest(user);
        return await ApiCall<UserCreateResponse, UserCreateRequest>(HttpMethod.Post, "user", null, userCreateRequest);
    }

    public async Task<List<Organization>> GetUserOrganizations(User user)
    {
        var organizations = new List<Organization>();
        if (user is not { Organizations.Count: > 0 }) return organizations;

        foreach (var orgCode in user.Organizations)
        {
            var organization = await GetOrganization(orgCode);
            organizations.Add(organization);
        }
        return organizations;
    }

    public async Task<Response> AddUserOrganization(string userId, string orgCode) => await AddOrganizationUsers(orgCode, new OrganizationUsersRequest { Users = [new() { Id = userId }] });

    public async Task<List<Response>> AddUserOrganizations(string userId, List<string> orgCodes)
    {
        var responses = new List<Response>();
        foreach (var orgCode in orgCodes)
        {
            var response = await AddUserOrganization(userId, orgCode);
            responses.Add(response);
        }
        return responses;
    }

    public async Task<List<Response>> RemoveUserOrganizations(string userId, List<string> orgCodes)
    {
        var responses = new List<Response>();
        foreach (var orgCode in orgCodes)
        {
            var response = await RemoveOrganizationUser(orgCode, userId);
            responses.Add(response);
        }
        return responses;
    }

    public async Task<UserMfa> GetUserMfa(string userId) => await GetEntity<UserMfa, UserMfaResponse>($"users/{userId}/mfa", nameof(UserMfaResponse.Mfa));

    public async Task<Response> ResetUserMfa(string userId, string mfaId) => await ApiCall<Response>(HttpMethod.Delete, $"users/{userId}/mfa/{mfaId}");

    /// <summary>
    /// Update the user, which is a comprehensive update that includes updating the user's identities.
    /// </summary>
    /// <param name="user">User object to update</param>
    /// <returns>Updated user (and identities)</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <remarks>
    /// The inbound user object will contain values that are form-specific and need to be converted to Kinde-specific structures 
    /// during the update process. This includes the user's identities, which are managed separately from the user object.
    /// Reference https://docs.kinde.com/manage-users/add-and-edit/add-manage-user-identities. 
    /// This documents how there is no update feature with identities. They require removal of the identity and re-adding.
    /// </remarks>
    public async Task<User> UpdateUser(User user)
    {
        if (string.IsNullOrEmpty(user.Id)) throw new ArgumentException($"{nameof(User)} {nameof(User.Id)} is required for updating.");

        var existingUser = await GetUser(user.Id);
        var existingIdentities = await GetUserIdentities(user.Id);

        // Update or add identities if they have changed
        if (user.Email != existingUser.Email)
        {
            var emailIdentity = new Identity { Type = "email", Value = user.Email };
            await AddOrUpdateUserIdentity(user.Id, emailIdentity);
        }

        // Delete username identity if blanked out, otherwise update if changed
        if (string.IsNullOrEmpty(user.Username))
        {
            var usernameIdentity = existingIdentities.FirstOrDefault(i => i.Type == "username");
            if (usernameIdentity is { Id: not null }) await DeleteIdentity(usernameIdentity.Id);
        }
        else if (user.Username != existingUser.Username)
        {
            var usernameIdentity = new Identity { Type = "username", Value = user.Username };
            await AddOrUpdateUserIdentity(user.Id, usernameIdentity);
        }

        // Delete phone identity if blanked out, otherwise update if changed
        if (string.IsNullOrEmpty(user.Phone))
        {
            var phoneIdentity = existingIdentities.FirstOrDefault(i => i.Type == "phone");
            if (phoneIdentity != null && phoneIdentity.Id != null) await DeleteIdentity(phoneIdentity.Id);
        }
        else if (user.Phone != existingUser.Phone)
        {
            var phoneIdentity = new Identity { Type = "phone", Value = user.Phone, PhoneCountryId=Constants.Identity.PhoneCountryId };
            await AddOrUpdateUserIdentity(user.Id, phoneIdentity);
        }

        // Update the user
        await ApiCall<User, User>(HttpMethod.Patch, $"user", null, user, ("id", user.Id));

        // Re-fetch the user
        var updatedUser = await GetUser(user.Id);

        return updatedUser;
    }

    public async Task<List<Role>> GetUserRoles(string userId, string orgCode) => await GetEntities<Role, RolesResponse>($"organizations/{orgCode}/users/{userId}/roles", nameof(RolesResponse.Roles));

    public async Task<bool> UserHasRole(string userId, string orgCode, string roleId)
    {
        var roles = await GetUserRoles(userId, orgCode);
        return roles.Any(r => r.Id == roleId);
    }

    public async Task<Response> ManageUserRole(User user, string? orgCode, Role role, bool removeRole = false)
    {
        if (user.Id == null) return new Response { Code = "Error", Message = "User ID is required" };
        if (string.IsNullOrEmpty(role.Id)) return new Response { Code = "Error", Message = $"Role {role.Name} with key {role.Key} not found" };
        if (string.IsNullOrEmpty(orgCode)) return new Response { Code = "Error", Message = $"User {user.Email} has no organization" };

        var hasRole = await UserHasRole(user.Id, orgCode, role.Id);

        if (!removeRole && hasRole) return new Response { Code = "OK", Message = $"User {user.Email} already has role {role.Key} in organization {orgCode}" };
        if (removeRole && !hasRole) return new Response { Code = "OK", Message = $"User {user.Email} already does not have role {role.Key} in organization {orgCode}" };
        if (removeRole) return await ApiCall<Response>(HttpMethod.Delete, $"organizations/{orgCode}/users/{user.Id}/roles/{role.Id}"); // Remove Role

        // Add Role
        return await ApiCall<Response, RoleAssign>(HttpMethod.Post, $"organizations/{orgCode}/users/{user.Id}/roles", null, new RoleAssign { Id = role.Id });
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Identity-Based Services
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    private async Task<List<Identity>> GetUserIdentities(string userId) => await GetEntities<Identity, IdentityResponse>($"users/{userId}/identities", nameof(IdentityResponse.Identities));

    private async Task<IdentityResponse> AddUserIdentity(string userId, Identity identity) => await ApiCall<IdentityResponse, Identity>(HttpMethod.Post, $"users/{userId}/identities", null, identity);
    
    public async Task<IdentityResponse> AddOrUpdateUserIdentity(string userId, Identity identity)
    {
        var identities = await GetUserIdentities(userId);
        var existingIdentity = identities.FirstOrDefault(i => i.Type == identity.Type);
        if (existingIdentity is { Id: not null }) await DeleteIdentity(existingIdentity.Id);
        return await AddUserIdentity(userId, identity);
    }

    public async Task<Response> DeleteIdentity(string identityId) => await ApiCall<Response>(HttpMethod.Delete, $"identities/{identityId}");

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Helpers
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    private async Task<List<TEntity>> GetEntities<TEntity, TResponse>(string apiName, string className, params (string Key, string Value)[] urlParameters) 
        where TResponse : class, new()
    {
        string? nextToken = null;
        var entities = new List<TEntity>();

        do
        {
            var response = await ApiCall<TResponse>(HttpMethod.Get, apiName, nextToken, urlParameters);
            if (typeof(TResponse).GetProperty(className)?.GetValue(response) is not List<TEntity> responseEntities || responseEntities.Count == 0) return entities;

            entities.AddRange(responseEntities);

            nextToken = typeof(TResponse).GetProperty("NextToken")?.GetValue(response) as string;
            if (string.IsNullOrEmpty(nextToken)) return entities;

        } while (true);
    }

    private async Task<TEntity> GetEntity<TEntity, TResponse>(string apiName, string className, params (string Key, string Value)[] urlParameters) 
        where TEntity : class, new()
        where TResponse : class, new()
    {
        var response = await ApiCall<TResponse>(HttpMethod.Get, apiName, null, urlParameters);

        return typeof(TResponse).GetProperty(className)?.GetValue(response) as TEntity ?? new TEntity();
    }

    private async Task<TOutput> ApiCall<TOutput>(HttpMethod httpMethod, string apiName)
        where TOutput : class, new()
    {
        return await ApiCall<TOutput, object>(httpMethod, apiName, null, null);
    }

    private async Task<TOutput> ApiCall<TOutput>(HttpMethod httpMethod, string apiName, string? nextToken, params (string Key, string Value)[] urlParameters) 
        where TOutput : class, new()
    {
        return await ApiCall<TOutput, object>(httpMethod, apiName, nextToken, null, urlParameters);
    }

    private async Task<TOutput> ApiCall<TOutput, TInput>(HttpMethod httpMethod, string apiName, string? nextToken, TInput? content, params (string Key, string Value)[] urlParameters)
        where TOutput : class, new()
    {
        var token = await GetAccessToken();
        if (token == null) return new TOutput(); // Houston, we got a problem

        var queryParameters = new List<string>();
        if (nextToken != null) queryParameters.Add($"next_token={nextToken}");

        foreach (var (key, value) in urlParameters) queryParameters.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");

        var uriPath = $"/api/v1/{apiName.TrimStart('/')}";

        if (queryParameters.Count > 0) uriPath += $"?{string.Join('&', queryParameters)}";

        var request = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = new Uri(uriPath, UriKind.Relative),
            Headers =
            {
                { "Authorization", $"Bearer {token}" }
            }
        };

        if (content != null)
        {
            var jsonContent = JsonConvert.SerializeObject(content);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Log.Information($"API call to {apiName} succeeded. Response: {body}");

            try
            {
                return JsonConvert.DeserializeObject<TOutput>(body) ?? new TOutput();
            }
            catch (JsonException ex)
            {
                Log.Error($"Failed to deserialize API response. Exception: {ex.Message}");
                return new TOutput();
            }
        }
        else
        {
            Log.Error("API call to {ApiName} failed. Response: {ResponseBody}", apiName, body);
            return new TOutput();
        }
    }

    private async Task<string?> GetAccessToken()
    {
        if (!string.IsNullOrEmpty(_token) && _tokenExpiration > DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60.0) return _token; // expire if within a minute of expiration

        var baseAddress = _httpClient.BaseAddress?.AbsoluteUri ?? string.Empty;
        var audUrl = Uri.EscapeDataString($"{baseAddress.TrimEnd('/')}/api");
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("/oauth2/token", UriKind.Relative),
            Content = new StringContent($"grant_type=client_credentials&client_id={_clientId}&client_secret={_clientSecret}&audience={audUrl}", Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseBody);
            var accessToken = accessTokenResponse?.AccessToken ?? string.Empty;
            _token = accessToken;

            if (!string.IsNullOrEmpty(accessToken))
            {
                var accessTokenPayload = DecodeJwtPayload(accessToken);
                var accessTokenObj = JsonConvert.DeserializeObject<AccessToken>(accessTokenPayload);
                _tokenExpiration = accessTokenObj?.Exp ?? 0;
            }

            Log.Information("Successfully retrieved access token.");
            return _token;
        }
        else
        {
            Log.Error($"Failed to retrieve access token. Response: {responseBody}");
            return null;
        }
    }

    private string DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) throw new ArgumentException("Invalid JWT token");

        var payload = parts[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        return Encoding.UTF8.GetString(jsonBytes);
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return System.Convert.FromBase64String(base64);
    }
}