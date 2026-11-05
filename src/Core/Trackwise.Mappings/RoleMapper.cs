using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class RoleMapper
{
    public static RoleDto ToDto(this AppRole entity)
    {
        return new RoleDto()
        {
            Name = entity.Name,
            DisplayName = entity.DisplayName
        };
    }

    public static RoleDto ToDto(this TenantRole entity)
    {
        return new RoleDto()
        {
            Name = entity.Name,
            DisplayName = entity.DisplayName
        };
    }
}
