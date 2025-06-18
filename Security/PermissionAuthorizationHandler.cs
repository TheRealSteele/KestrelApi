using Microsoft.AspNetCore.Authorization;

namespace KestrelApi.Security;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);
        
        // Check for permissions claims (Auth0 can send as array of claims)
        var permissionClaims = context.User.FindAll("permissions").ToList();
        
        if (permissionClaims.Count > 0)
        {
            // Check each permission claim
            foreach (var claim in permissionClaims)
            {
                // Single claim might contain comma-separated values
                var permissions = claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim());
                
                if (permissions.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }
}