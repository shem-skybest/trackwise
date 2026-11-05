using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CancelWorkfileValidator : AbstractValidator<CancelWorkfileCommand>
{
    public CancelWorkfileValidator()
    {
        RuleFor(i => i.Reason).NotEmpty().MaximumLength(1000);
    }
}
