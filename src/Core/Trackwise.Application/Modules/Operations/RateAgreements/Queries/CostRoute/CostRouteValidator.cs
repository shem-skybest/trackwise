using FluentValidation;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

internal sealed class CostRouteValidator : AbstractValidator<CostRouteQuery>
{
    public CostRouteValidator()
    {
        RuleFor(q => q.TruckSize).IsInEnum();
        RuleFor(q => q.GarageToWarehouseKm).GreaterThanOrEqualTo(0);
        RuleFor(q => q.WarehouseToDropKm).GreaterThanOrEqualTo(0);
        RuleFor(q => q.DropToGarageKm).GreaterThanOrEqualTo(0);
        RuleFor(q => q.DieselPricePerLitre).GreaterThan(0).When(q => q.DieselPricePerLitre.HasValue);
        RuleFor(q => q.KmPerLitre).GreaterThan(0).When(q => q.KmPerLitre.HasValue);
        RuleFor(q => q.RoundUpTo).GreaterThanOrEqualTo(0).When(q => q.RoundUpTo.HasValue);
    }
}
