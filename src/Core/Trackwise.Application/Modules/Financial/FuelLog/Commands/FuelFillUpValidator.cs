using FluentValidation;

namespace Trackwise.Application.Modules.Financial.FuelLog.Commands;

internal abstract class FuelFillUpValidator<T> : AbstractValidator<T>
    where T : FuelFillUpCommand
{
    protected FuelFillUpValidator()
    {
        RuleFor(c => c.TruckId).NotEmpty().WithMessage("Pick the truck.");
        RuleFor(c => c.FilledAt).NotEmpty().WithMessage("Enter when the tank was filled.");
        RuleFor(c => c.Liters).GreaterThan(0).LessThanOrEqualTo(1000)
            .WithMessage("Liters must be above zero and at most 1,000.");
        RuleFor(c => c.Amount).GreaterThan(0).WithMessage("Enter the amount paid.");
        RuleFor(c => c.FleetCard).IsInEnum().When(c => c.FleetCard.HasValue);
        RuleFor(c => c.OdometerReading).InclusiveBetween(0, 9_999_999).When(c => c.OdometerReading.HasValue);
        RuleFor(c => c.Station).MaximumLength(255);
        RuleFor(c => c.Notes).MaximumLength(2000);
        RuleFor(c => c.ReceiptBlobPath).MaximumLength(500);
    }
}
