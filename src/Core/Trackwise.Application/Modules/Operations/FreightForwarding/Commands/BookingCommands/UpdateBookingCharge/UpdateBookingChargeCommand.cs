using FluentValidation;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.UpdateBookingCharge;

public sealed record UpdateBookingChargeCommand(
    Guid BookingId,
    Guid ChargeId,
    string Description,
    decimal Quantity,
    decimal UnitBuying,
    decimal UnitSelling,
    string Reason) : ICommand<Result<BookingChargeDto>>;

public sealed class UpdateBookingChargeCommandValidator : AbstractValidator<UpdateBookingChargeCommand>
{
    public UpdateBookingChargeCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.ChargeId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0m).PrecisionScale(18, 4, ignoreTrailingZeros: true);
        RuleFor(x => x.UnitBuying).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.UnitSelling).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}
