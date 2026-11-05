using FluentValidation;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class RateAgreementLineValidator : AbstractValidator<RateAgreementLineDto>
{
    public RateAgreementLineValidator()
    {
        RuleFor(l => l.Label).NotEmpty().MaximumLength(200);
        RuleFor(l => l.Amount).GreaterThan(0);
        RuleFor(l => l.Tonnage).IsInEnum().When(l => l.Tonnage.HasValue);
        RuleFor(l => l.City).MaximumLength(100);
        RuleFor(l => l.Province).MaximumLength(100);
        RuleFor(l => l.FuelPercent).InclusiveBetween(0, 100).When(l => l.FuelPercent.HasValue);

        RuleForEach(l => l.FuelSurcharges).ChildRules(s =>
        {
            s.RuleFor(x => x.DieselPrice).GreaterThan(0);
            s.RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        });
    }
}
