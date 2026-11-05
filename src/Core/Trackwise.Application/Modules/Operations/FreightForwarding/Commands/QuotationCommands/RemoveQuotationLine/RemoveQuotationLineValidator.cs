using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RemoveQuotationLine;

using FluentValidation;

public sealed class RemoveQuotationLineValidator : AbstractValidator<RemoveQuotationLineCommand>
{
    public RemoveQuotationLineValidator()
    {
        RuleFor(x => x.QuotationId)
            .NotEmpty()
            .WithMessage("Quotation ID is required.");

        RuleFor(x => x.LineId)
            .NotEmpty()
            .WithMessage("Quotation line ID is required.");
    }
}
