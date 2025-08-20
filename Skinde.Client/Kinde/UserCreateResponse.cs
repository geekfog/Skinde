using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class UserCreateResponse
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("created")]
    public bool IsCreated { get; set; } = false;
}