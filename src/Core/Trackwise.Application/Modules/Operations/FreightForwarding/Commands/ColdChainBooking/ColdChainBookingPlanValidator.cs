using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>Rules create, update and estimate share.</summary>
internal abstract class ColdChainBookingPlanValidator<T> : AbstractValidator<T>
    where T : ColdChainBookingPlanRequest
{
    protected ColdChainBookingPlanValidator()
    {
        RuleFor(i => i.ServiceDate).NotEmpty();
        RuleFor(i => i.TruckSize).IsInEnum();
        RuleFor(i => i.Commodity).MaximumLength(200);
        RuleFor(i => i.Notes).MaximumLength(1000);
        RuleFor(i => i.TemperatureMinC).InclusiveBetween(-40, 30).When(i => i.TemperatureMinC.HasValue);
        RuleFor(i => i.TemperatureMaxC).InclusiveBetween(-40, 30).When(i => i.TemperatureMaxC.HasValue);

        RuleFor(i => i.TemperatureMaxC)
            .GreaterThanOrEqualTo(i => i.TemperatureMinC!.Value)
            .WithMessage("The highest temperature can't be below the lowest.")
            .When(i => i.TemperatureMinC.HasValue && i.TemperatureMaxC.HasValue);

        RuleFor(i => i.Drops).NotEmpty().WithMessage("Add at least one drop.");
        RuleFor(i => i.Drops)
            .Must(drops => drops.Select(d => d.SiteId).Distinct().Count() == drops.Count)
            .WithMessage("Each delivery location can only be one drop on a booking.");

        RuleForEach(i => i.Drops).ChildRules(drop =>
        {
            drop.RuleFor(d => d.SiteId).NotEmpty().WithMessage("Pick a delivery location for every drop.");
            drop.RuleFor(d => d.PlannedBoxes).GreaterThanOrEqualTo(0).When(d => d.PlannedBoxes.HasValue);
            drop.RuleFor(d => d.PlannedBags).GreaterThanOrEqualTo(0).When(d => d.PlannedBags.HasValue);
            drop.RuleFor(d => d.Notes).MaximumLength(500);
        });
    }
}
