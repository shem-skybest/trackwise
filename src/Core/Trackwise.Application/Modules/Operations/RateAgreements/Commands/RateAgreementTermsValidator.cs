using FluentValidation;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>Rules create and update share. Line shape (band xor place) is the entity's to refuse.</summary>
internal abstract class RateAgreementTermsValidator<T> : AbstractValidator<T>
    where T : RateAgreementTermsCommand
{
    protected RateAgreementTermsValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.FuelMethod).IsInEnum();
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleFor(i => i.IncludedDrops).GreaterThanOrEqualTo(1).When(i => i.IncludedDrops.HasValue);
        RuleFor(i => i.FoulTripPercent).InclusiveBetween(0, 100).When(i => i.FoulTripPercent.HasValue);
        RuleFor(i => i.BackloadPercent).InclusiveBetween(0, 100).When(i => i.BackloadPercent.HasValue);
        RuleFor(i => i.DemurrageFreeHours).GreaterThanOrEqualTo(0).When(i => i.DemurrageFreeHours.HasValue);
        RuleFor(i => i.DemurrageHourlyRate).GreaterThanOrEqualTo(0).When(i => i.DemurrageHourlyRate.HasValue);
        RuleFor(i => i.OtherLocationCharge).GreaterThanOrEqualTo(0).When(i => i.OtherLocationCharge.HasValue);
        RuleForEach(i => i.ExtraDropCharges).ChildRules(c => c.RuleFor(x => x.Amount).GreaterThanOrEqualTo(0));
        RuleFor(i => i.ExtraDropCharges)
            .Must(charges => charges.Select(c => c.Tonnage).Distinct().Count() == charges.Count)
            .WithMessage("Give each truck size one extra-drop charge.");

        RuleFor(i => i.EffectiveTo)
            .GreaterThanOrEqualTo(i => i.EffectiveFrom)
            .WithMessage("The agreement can't end before it starts.")
            .When(i => i.EffectiveTo.HasValue);

        RuleForEach(i => i.Lines).SetValidator(new RateAgreementLineValidator());
    }
}
