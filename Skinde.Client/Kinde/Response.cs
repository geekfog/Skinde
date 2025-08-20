using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class Response
{
    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }

    public bool IsSuccessful => !string.IsNullOrEmpty(Code) && SuccessCodes.Any(code => Code.Contains(code, StringComparison.CurrentCultureIgnoreCase));

    private static readonly List<string> SuccessCodes = new()
    {
        "OK",
        "updated",
        "added"
    };
}