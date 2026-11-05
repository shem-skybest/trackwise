using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class ClearWorkfileReferenceClosureValidator
    : AbstractValidator<ClearWorkfileReferenceClosureCommand>
{
    public ClearWorkfileReferenceClosureValidator()
    {
        RuleFor(i => i.Remarks).MaximumLength(2000);
    }
}
