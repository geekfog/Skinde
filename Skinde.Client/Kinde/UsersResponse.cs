using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class UsersResponse : RetrieveBase
{
    [JsonProperty("users")]
    public List<User> Users { get; set; } = new();
}