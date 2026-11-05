using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.AddBookingCharge;

/// <summary>
/// Adds a charge that was not in the quotation (detention, extra handling, a discount...).
/// Unit amounts are positive; a Discount category is negated by the domain, same as quotation lines.
/// </summary>
public sealed record AddBookingChargeCommand(
    Guid BookingId,
    ChargeCategory Category,
    string Description,
    decimal Quantity,
    decimal UnitBuying,
    decimal UnitSelling,
    string Reason) : ICommand<Result<BookingChargeDto>>;   // same base interface as your other commands

public sealed class AddBookingChargeCommandValidator : AbstractValidator<AddBookingChargeCommand>
{
    public AddBookingChargeCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();

        // Lengths and precision match BookingChargeLineConfiguration so the database never rejects
        // what validation let through.
        RuleFor(x => x.Description).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Say why the charge is being added; it appears on the booking and the invoice trail.");

        RuleFor(x => x.Quantity).GreaterThan(0m).PrecisionScale(18, 4, ignoreTrailingZeros: true);
        RuleFor(x => x.UnitBuying).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.UnitSelling).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}
