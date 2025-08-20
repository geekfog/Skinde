using Microsoft.AspNetCore.Components.Authorization;
using Skinde.Ui.Services.Interfaces;
using Skinde.Utility;
using Skinde.Utility.Constants;

namespace Skinde.Ui.Services
{
    public class UserService(AuthenticationStateProvider authenticationStateProvider) : IUserService
    {
        private const string DefaultInitials = "N/A";

        public async Task<string> GetInitialsAsync()
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated != true)
                return DefaultInitials;

            return user.Claims.FirstOrDefault(claim => claim.Type == Claims.FullName)?.Value.GetInitials() ?? DefaultInitials;
        }
    }
}