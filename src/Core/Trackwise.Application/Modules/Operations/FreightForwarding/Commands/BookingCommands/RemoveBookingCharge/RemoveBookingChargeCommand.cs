using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.RemoveBookingCharge;

public sealed record RemoveBookingChargeCommand(Guid BookingId, Guid ChargeId) : ICommand<Result>;
