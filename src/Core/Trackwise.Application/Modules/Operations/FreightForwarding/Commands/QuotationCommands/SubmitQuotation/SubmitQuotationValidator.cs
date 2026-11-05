using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.SubmitQuotation;

internal sealed class SubmitQuotationValidator : AbstractValidator<SubmitQuotationCommand>
{
    public SubmitQuotationValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Quotation ID is required.");
    }
}
