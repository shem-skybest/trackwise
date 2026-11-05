using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.IssueQuotation;

internal sealed class IssueQuotationValidator : AbstractValidator<IssueQuotationCommand>
{
    public IssueQuotationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ValidityDays)
            .InclusiveBetween(1, 90)
            .WithMessage("Validity period must be between 1 and 90 days.");
    }
}
