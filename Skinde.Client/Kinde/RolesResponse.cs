using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class RolesResponse : RetrieveBase
{
    [JsonProperty("roles")]
    public List<Role> Roles { get; set; } = new();
}