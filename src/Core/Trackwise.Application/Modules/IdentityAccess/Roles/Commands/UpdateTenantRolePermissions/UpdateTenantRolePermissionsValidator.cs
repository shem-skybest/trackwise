using FluentValidation;
using Trackwise.Shared.Identity.Policies;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

internal sealed class UpdateTenantRolePermissionsValidator : AbstractValidator<UpdateTenantRolePermissionsCommand>
{
    public UpdateTenantRolePermissionsValidator()
    {
        RuleFor(i => i.RoleName).NotEmpty();
        RuleForEach(i => i.Permissions)
            .Must(p => TenantRolePermissions.Catalog.Contains(p))
            .WithMessage("'{PropertyValue}' is not a permission a staff role can hold.");
    }
}
