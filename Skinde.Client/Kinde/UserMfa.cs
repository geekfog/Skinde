using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class UserMfa
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("created_on")]
    public DateTime? CreatedOn { get; set; }

    [JsonProperty("is_verified")]
    public bool IsVerified { get; set; } = false;

    [JsonProperty("usage_count")]
    public int UsageCount { get; set; }

    [JsonProperty("last_used_on")]
    public DateTime? LastUsedOn { get; set; }
}