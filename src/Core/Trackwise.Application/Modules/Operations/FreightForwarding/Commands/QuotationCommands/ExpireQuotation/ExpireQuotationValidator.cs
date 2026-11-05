using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ExpireQuotation;

internal sealed class ExpireQuotationValidator : AbstractValidator<ExpireQuotationCommand>
{
    public ExpireQuotationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
