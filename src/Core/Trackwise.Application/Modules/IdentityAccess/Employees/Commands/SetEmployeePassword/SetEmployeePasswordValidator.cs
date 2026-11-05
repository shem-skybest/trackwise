using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

internal sealed class SetEmployeePasswordValidator : AbstractValidator<SetEmployeePasswordCommand>
{
    public SetEmployeePasswordValidator()
    {
        RuleFor(i => i.UserId).NotEmpty();
        RuleFor(i => i.Password).NotEmpty().MaximumLength(128);
    }
}
