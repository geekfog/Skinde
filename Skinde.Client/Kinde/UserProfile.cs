using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class UserProfile
{
    [JsonProperty("given_name")]
    public string? FirstName { get; set; }

    [JsonProperty("family_name")]
    public string? LastName { get; set; }

    [JsonProperty("picture")]
    public string? PictureUrl { get; set; }
}