using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class User
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// User's username identity
    /// </summary>
    /// <remarks>
    /// This is similar to the <see cref="PreferredEmail"/> property to quickly get the username from the GetUser API and not requiring population from the username identity.
    /// </remarks>
    [JsonProperty("username")]
    public string? Username { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    /// <summary>
    /// User's primary email address (is_primary identity)
    /// </summary>
    /// <remarks>
    /// This is similar to the <see cref="Username"/> property to quickly get the user's email from the GetUser API call and not requiring population from the email identity.
    /// </remarks>
    [JsonProperty("preferred_email")]
    public string? PreferredEmail { set => Email = value; }

    private string? _phone;
    public string? Phone
    {
        get => _phone;
        set => _phone = value?.StartsWith("+1") == true ? value[2..] : value;
    }

    [JsonProperty("first_name")]
    public string? FirstName { get; set; }

    [JsonProperty("last_name")]
    public string? LastName { get; set; }

    [JsonProperty("given_name")]
    public string? GivenName => FirstName;

    [JsonProperty("family_name")]
    public string? FamilyName => LastName;

    [JsonProperty("picture")]
    private string? PictureSetter { set => PictureUrl = value; }
    [JsonIgnore]
    public string? PictureUrl { get; set; }

    [JsonProperty("joined_on")]
    private DateTime JoinedSetter { set => Joined = value; }
    [JsonIgnore]
    public DateTime Joined { get; set; }

    [JsonProperty("roles")]
    private List<string> RolesSetter { set => Roles = value; }
    [JsonIgnore]
    public List<string> Roles { get; set; } = [];

    [JsonProperty("created_on")]
    private DateTime? CreatedSetter { set => Created = value; }
    [JsonIgnore]
    public DateTime? Created { get; set; }

    [JsonProperty("last_signed_in")]
    private DateTime? LastLoginSetter { set => LastLogin = value; }
    [JsonIgnore]
    public DateTime? LastLogin { get; set; }

    [JsonProperty("is_suspended")]
    public bool IsSuspended { get; set; }

    [JsonProperty("is_password_reset_requested")]
    public bool IsPasswordResetRequested { get; set; }

    [JsonProperty("total_sign_ins")]
    private int TotalLoginsSetter { set => TotalLogins = value; }
    [JsonIgnore]
    public int TotalLogins { get; set; }

    [JsonProperty("failed_sign_ins")]
    private int FailedLoginsSetter { set => FailedLogins = value; }
    [JsonIgnore]
    public int FailedLogins { get; set; }

    [JsonProperty("organizations")]
    private List<string> OrganizationsSetter { set => Organizations = value; }
    [JsonIgnore]
    public List<string> Organizations { get; set; } = new();

    public string GetAnOrganizationCode()
    {
        return Organizations.FirstOrDefault() ?? string.Empty;
    }

    public bool HasAnOrganizationCode()
    {
        return Organizations.Count > 0;
    }
}