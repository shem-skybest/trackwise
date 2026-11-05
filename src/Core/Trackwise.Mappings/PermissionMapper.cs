using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class PermissionMapper
{
    public static PermissionDto ToDto(this AppRoleClaim entity)
    {
        return new PermissionDto()
        {
            Name = entity.ClaimValue
        };
    }

    public static PermissionDto ToDto(this TenantRoleClaim entity)
    {
        return new PermissionDto()
        {
            Name = entity.ClaimValue
        };
    }
}
