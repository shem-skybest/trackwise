using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.RemoveTariffRate;

internal sealed class RemoveTariffRateValidator : AbstractValidator<RemoveTariffRateCommand>
{
    public RemoveTariffRateValidator()
    {
        RuleFor(i => i.RateId).NotEmpty();
    }
}
