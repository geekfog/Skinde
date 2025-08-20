using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class RoleAssign
{
    [JsonProperty("role_id")]
    public string? Id { get; set; }
}