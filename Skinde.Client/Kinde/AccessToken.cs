using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class AccessToken
{
    [JsonProperty("aud")]
    public List<string>? Aud { get; set; }

    [JsonProperty("azp")]
    public string? Azp { get; set; }

    [JsonProperty("exp")]
    public long Exp { get; set; }

    [JsonProperty("gty")]
    public List<string>? Gty { get; set; }

    [JsonProperty("iat")]
    public long Iat { get; set; }

    [JsonProperty("iss")]
    public string? Iss { get; set; }

    [JsonProperty("jti")]
    public string? Jti { get; set; }

    [JsonProperty("scope")]
    public string? Scopes { get; set; }
}