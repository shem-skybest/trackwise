using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RejectQuotation;

internal sealed class RejectQuotationValidator : AbstractValidator<RejectQuotationCommand>
{
    public RejectQuotationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RejectedBy).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
