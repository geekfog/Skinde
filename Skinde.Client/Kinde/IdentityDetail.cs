using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class IdentityDetail
{
    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("username")]
    public string? Username { get; set; }

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Only required when <see cref="Phone"/> is populated as populating as part of creating a new user.
    /// </summary>
    [JsonProperty("phone_country_id")]
    public string? PhoneCountryId { get; set; }
}