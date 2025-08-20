using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class Organization
{
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("handle")]
    public string? Handle { get; set; }

    [JsonProperty("is_default")]
    private bool IsDefaultSetter { set => IsDefault = value; }
    [JsonIgnore]
    public bool IsDefault { get; set; } = false;

    [JsonProperty("external_id")]
    public string? ExternalId { get; set; }

    [JsonProperty("is_auto_membership_enabled")]
    public bool IsAutoMemberShipEnabled { get; set; } = false;

    // This is deprecated, but needed for Updating the Orgaization, but not used for retrieving the organization (in transition)
    [JsonProperty("is_allow_registrations")]
    private bool IsAllowRegistrations
    { 
        get => IsAutoMemberShipEnabled;
        set => IsAutoMemberShipEnabled = value;
    }

    [JsonProperty("created_on")]
    private DateTime? CreatedSetter { set => Created = value; }
    [JsonIgnore]
    public DateTime? Created { get; set; }

    // Potenal extension of Newtonsoft.Json that adds coding support for serialization logic
    //public bool ShouldSerializeCode()
    //{
    //    return !string.IsNullOrEmpty(Code);
    //}
}