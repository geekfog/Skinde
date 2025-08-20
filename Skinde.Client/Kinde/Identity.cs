using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class Identity
{
    /// <summary>
    /// Used for identity calls (response)
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Used for identity calls (response) and user creation (request). Allowed values are email, username, phone, enterprise, and social.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Only required when identity type is phone (request) and calling the Create Identity API endpoint. Don't include while Creating a new user via <see cref="UserCreateRequest"/>.
    /// </summary>
    [JsonProperty("phone_country_id")]
    public string? PhoneCountryId { get; set; }

    /// <summary>
    /// The email address, social identity, phone number, or username of the user when updating identity (request)
    /// </summary>
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Essentially the same as <see cref="Value"/> but used for identity calls (response) return object"/>
    /// </summary>
    [JsonProperty("name")]
    public string? Name { set => Value = value; }

    /// <summary>
    /// Whether the identity type is the primary (request)
    /// </summary>
    [JsonProperty("is_primary")]
    public bool? IsPrimary { get; set; }

    /// <summary>
    /// Used for identity calls (response)
    /// </summary>
    [JsonProperty("details")]
    public IdentityDetail Details { get; set; } = new IdentityDetail();
}