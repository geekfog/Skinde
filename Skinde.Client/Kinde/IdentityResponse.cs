using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class IdentityResponse : RetrieveBase
{
    /// <summary>
    /// Used to create or update identities
    /// </summary>
    [JsonProperty("identity")]
    public Identity Identity { get; set; } = new();

    /// <summary>
    /// Used to retrieve identities
    /// </summary>
    [JsonProperty("identities")]
    public List<Identity> Identities { get; set; } = new();
}