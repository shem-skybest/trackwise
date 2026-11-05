using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

internal sealed class SetDriverDeviceTokenValidator : AbstractValidator<SetDriverDeviceTokenCommand>
{
    public SetDriverDeviceTokenValidator()
    {
        RuleFor(i => i.UserId).NotEmpty();
        RuleFor(i => i.DeviceToken).NotEmpty();
    }
}
