using Microsoft.AspNetCore.Authorization;
using Skinde.Utility.Constants;

namespace Skinde.Ui.Auth;

public class ProductAccessHandler : AuthorizationHandler<ProductAccessRequirement>
{
    private readonly string _adminRoleKey;

    public ProductAccessHandler(IConfiguration configuration)
    {
        // Try to get the role from configuration, fallback to default if not found or empty
        _adminRoleKey = configuration[Kinde.SettingAdminAccessKey] ?? Kinde.DefaultAdminAccessKey;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProductAccessRequirement requirement)
    {
        if (context.User.IsInRole(_adminRoleKey)) // Role-based product access
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}