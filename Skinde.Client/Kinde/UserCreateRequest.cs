using Newtonsoft.Json;

namespace Skinde.Client.Kinde;

public class UserCreateRequest
{
    [JsonProperty("profile")]
    public UserProfile Profile { get; set; } = new UserProfile();

    [JsonProperty("organization_code")]
    public string? OrganizationCode { get; set; }

    [JsonProperty("provided_id")]
    public string? ExternalId { get; set; }

    [JsonProperty("identities")]
    public List<Identity> Identities { get; set; } = new List<Identity>();

    public UserCreateRequest(User user)
    {
        Profile.FirstName = user.FirstName;
        Profile.LastName = user.LastName;

        if (user.Organizations.Count != 0)
            OrganizationCode = user.Organizations.First();

        // Required field
        Identities.Add(new Identity
        {
            Type = "email",
            Details = new IdentityDetail
            {
                Email = user.Email
            }
        });

        if (!string.IsNullOrEmpty(user.Username))
        {
            Identities.Add(new Identity
            {
                Type = "username",
                Details = new IdentityDetail
                {
                    Username = user.Username
                }
            });
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            Identities.Add(new Identity
            {
                Type = "phone",
                Details = new IdentityDetail
                {
                    Phone = user.Phone,
                    PhoneCountryId = Constants.Identity.PhoneCountryId
                }
            });
        }
    }
}