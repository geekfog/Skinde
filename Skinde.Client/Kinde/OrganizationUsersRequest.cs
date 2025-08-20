using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class OrganizationUsersRequest
{
    [JsonProperty("users")]
    public List<User> Users { get; set; } = new();
}