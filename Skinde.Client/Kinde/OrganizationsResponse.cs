using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class OrganizationsResponse : RetrieveBase
{
    [JsonProperty("organizations")]
    public List<Organization> Organizations { get; set; } = new();
}