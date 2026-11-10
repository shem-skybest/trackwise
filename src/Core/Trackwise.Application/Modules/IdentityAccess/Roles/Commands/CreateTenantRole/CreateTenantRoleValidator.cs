using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

internal sealed class CreateTenantRoleValidator : AbstractValidator<CreateTenantRoleCommand>
{
    public CreateTenantRoleValidator()
    {
        RuleFor(i => i.DisplayName).NotEmpty().MaximumLength(60);
        RuleFor(i => i.CopyFromRoleName).NotEmpty();
    }
}
