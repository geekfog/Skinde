using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class AccessTokenResponse
{
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }
}