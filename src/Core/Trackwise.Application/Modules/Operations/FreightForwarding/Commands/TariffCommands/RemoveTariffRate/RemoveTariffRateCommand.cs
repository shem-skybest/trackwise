using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.RemoveTariffRate;

public record RemoveTariffRateCommand(Guid RateId) : ICommand<Result>;
