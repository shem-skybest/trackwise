using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Commands;

public class ImpersonateUserValidator : AbstractValidator<ImpersonateUserCommand>
{
    public ImpersonateUserValidator()
    {
        RuleFor(x => x.TargetEmail)
            .NotEmpty().WithMessage("Target email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.MasterPassword)
            .NotEmpty().WithMessage("Master password is required.");
    }
}
