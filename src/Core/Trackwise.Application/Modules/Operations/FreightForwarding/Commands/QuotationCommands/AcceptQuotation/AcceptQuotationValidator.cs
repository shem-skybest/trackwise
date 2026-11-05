using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AcceptQuotation;

internal sealed class AcceptQuotationValidator : AbstractValidator<AcceptQuotationCommand>
{
    public AcceptQuotationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
