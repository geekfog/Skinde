using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Skinde.Ui.Components;
using Skinde.Utility;
using Skinde.Utility.Constants;
using Skinde.Client;
using MudBlazor.Services;
using Skinde.Ui.Services.Interfaces;
using Skinde.Ui.Services;
using Skinde.Ui.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Skinde.Ui;

public static class Program
{
    public static void Main(string[] args)
    {
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Build Services
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        var builder = WebApplication.CreateBuilder(args);

        // Add support for secrets.json for development (and ASPNETCORE_ENVIRONMENT is not set to the value Development)
        if (Server.IsDevelopment) builder.Configuration.AddUserSecrets(nameof(Skinde));

        // Add Serilog Support
        builder.Services.AddSerilog();
        SerilogHelper.Configure(builder.Environment);

        // Add Azure KeyVault configuration
        var keyVaultEndpoint = builder.Setting("KeyVaultEndpoint");
        if (!string.IsNullOrEmpty(keyVaultEndpoint))
        {
            var keyVaultEndpointUrl = new Uri(keyVaultEndpoint);
            try
            {
                builder.Configuration.AddAzureKeyVault(keyVaultEndpointUrl, new DefaultAzureCredential());
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding KeyVault configuration: {ex.Message}");
            }           
        }

        if (Server.IsProduction)
        {
            builder.Services.AddHsts(options => // https://aka.ms/aspnetcore-hsts.
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(365);
                });
        }

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Dependency Injection
        builder.Services.AddMudServices(); // Add MudBlazor
        builder.Services.AddHttpClient(Client.Constants.ApiClient.Kinde, client =>
        {
            client.BaseAddress = new Uri(builder.Configuration.Setting(SettingName.KindeApiBaseUrl));
        });
        builder.Services.AddSingleton<IAuthorizationHandler, ProductAccessHandler>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<KindeService>();
        builder.Services.AddScoped<IUserService, UserService>();

        // Configure Newtonsoft.Json to ignore null values globally
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        // Local Development-based Web Static Asset Access
        if (Server.IsDevelopment)
            builder.WebHost.UseStaticWebAssets(); // This is needed to support local running via Visual Studio with launchSettings.json with ASPNETCORE_ENVIRONMENT not set to "Development"

        // Authentication/Authorization Support
        builder.AddKindeAuth();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.SystemAdminAccess, policy => policy.AddRequirements(new ProductAccessRequirement()));
        });
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Build Application
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (Server.IsProduction)
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            Secure = CookieSecurePolicy.Always
        });

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }

    public static void AddKindeAuth(this WebApplicationBuilder builder)
    {
        // Get Setting values
        var authUrl = builder.Setting("OAuth2:AuthUrl");
        var clientId = builder.Setting("OAuth2:ClientId");
        var clientSecret = builder.Setting("OAuth2:ClientSecret", true);
        var scopes = builder.Setting("OAuth2:Scopes");

        builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.AccessDeniedPath = "/Error";
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.SlidingExpiration = true;
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authUrl;
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.ClaimActions.MapAll();
            options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Name, "preferred_username");

            foreach (var scope in scopes.Split(' ')) options.Scope.Add(scope);

            // Map roles (specific JSON payload) from the access token (OnTokenValidated vs OnTokenResponseReceived)
            options.Events.OnTokenValidated = context =>
            {
                Log.Information($"{nameof(AddKindeAuth)} OnTokenResponseReceived Called");

                // Extract the access token from the context
                var accessToken = context.TokenEndpointResponse?.AccessToken;

                // Decode and parse the JWT payload to extract roles
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;
                var payload = jsonToken?.Payload.SerializeToJson();
                if (payload == null) return Task.CompletedTask;

                using var document = JsonDocument.Parse(payload);

                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;

                // Extract the roles array from the JSON payload
                if (document.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var roleElement in rolesElement.EnumerateArray())
                    {
                        if (!roleElement.TryGetProperty("key", out var keyElement)) continue;

                        var roleKey = keyElement.GetString();
                        if (roleKey != null) claimsIdentity?.AddClaim(new Claim(ClaimTypes.Role, roleKey)); // Add the role key as a claim
                    }
                }

                // Extract feature flags from the JSON payload
                if (document.RootElement.TryGetProperty("feature_flags", out var featureFlagsElement) && featureFlagsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var featureFlag in featureFlagsElement.EnumerateObject())
                    {
                        var flagName = featureFlag.Name;
                        if (!featureFlag.Value.TryGetProperty("v", out var valueElement)) continue;

                        var flagValue = valueElement.ToString();
                        claimsIdentity?.AddClaim(new Claim(Claims.FeatureFlag, $"{flagName}={flagValue}")); // Add each feature flag as a claim
                    }
                }

                return Task.CompletedTask;
            };
        });
    }
}