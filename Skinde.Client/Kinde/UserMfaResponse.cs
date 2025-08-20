using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class UserMfaResponse : RetrieveBase
{
    [JsonProperty("mfa")]
    public UserMfa? Mfa { get; set; }
}