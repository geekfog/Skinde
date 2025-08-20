using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class Role
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("key")]
    public string? Key { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("is_default_role")]
    public bool IsDefault { get; set; } = false;
}