using FluentValidation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.UpdateQuotation;

public class UpdateQuotationValidator : AbstractValidator<UpdateQuotationCommand>
{
    public UpdateQuotationValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Quotation id is required.");

        RuleFor(x => x.Data)
            .NotNull()
            .WithMessage("Quotation details are required.")
            .SetValidator(new CreateQuotationValidator());
    }
}
