using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CloseWorkfileValidator : AbstractValidator<CloseWorkfileCommand>
{
    public CloseWorkfileValidator()
    {
        RuleFor(i => i.Remarks).MaximumLength(2000);
    }
}
