using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class RetrieveBase
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("next_token")]
    public string NextToken { get; set; } = string.Empty;
}