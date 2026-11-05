#nullable enable

using System.Security.Claims;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.IdentityServer.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using CustomClaimTypes = Trackwise.Shared.Identity.Claims.CustomClaimTypes;

namespace Trackwise.IdentityServer.Services;

public class UserCustomClaimsFactory(
    UserManager<User> userManager,
    RoleManager<AppRole> roleManager,
    IOptions<IdentityOptions> options,
    IHttpContextAccessor httpContextAccessor,
    ITenantUnitOfWork tenantUow)
    : UserClaimsPrincipalFactory<User, AppRole>(userManager, roleManager, options)
{
    private readonly HttpContext _httpContext = httpContextAccessor.HttpContext!;

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var claimsIdentity = await base.GenerateClaimsAsync(user);
        var tenantId = _httpContext.GetTenantId() ?? user.TenantId;

        AddProfileClaims(claimsIdentity, user);
        await AddAppRoleClaimsAsync(claimsIdentity, user);

        if (tenantId is null)
        {
            return claimsIdentity;
        }

        await tenantUow.SetCurrentTenantByIdAsync(tenantId.Value);

        // Deliberately filtered, not IgnoreQueryFilters(): a soft-deleted or merged-away Employee
        // must resolve to null here exactly as a hard-deleted one always has, so this login picks up
        // no tenant role claim. Reading past the filter would silently reinstate access for someone
        // a steward removed.
        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(user.Id);

        claimsIdentity.AddClaim(new Claim(CustomClaimTypes.Tenant, tenantId.Value.ToString()));
        await AddTenantRoleClaimsAsync(claimsIdentity, employee);

        if (!string.IsNullOrEmpty(employee?.JobTitle))
        {
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.JobTitle, employee.JobTitle));
        }

        return claimsIdentity;
    }

    private async Task AddAppRoleClaimsAsync(ClaimsIdentity claimsIdentity, User user)
    {
        var roles = await UserManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.Role, role));
        }
    }

    private static Task AddTenantRoleClaimsAsync(ClaimsIdentity claimsIdentity, Employee? employee)
    {
        if (employee?.Role is not null)
        {
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.Role, employee.Role.Name));
        }

        return Task.CompletedTask;
    }

    private static void AddProfileClaims(ClaimsIdentity claimsIdentity, User user)
    {
        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claimsIdentity.AddClaim(new Claim("given_name", user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claimsIdentity.AddClaim(new Claim("family_name", user.LastName));
        }
    }
}
