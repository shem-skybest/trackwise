using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class ReturnWorkfileToOpsValidator : AbstractValidator<ReturnWorkfileToOpsCommand>
{
    public ReturnWorkfileToOpsValidator()
    {
        RuleFor(i => i.Reason).NotEmpty().MaximumLength(1000);
    }
}
