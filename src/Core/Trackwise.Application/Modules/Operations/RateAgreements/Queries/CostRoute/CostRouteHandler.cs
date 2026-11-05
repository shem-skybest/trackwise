using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

internal sealed class CostRouteHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CostRouteQuery, Result<RouteCostingDto>>
{
    public async Task<Result<RouteCostingDto>> Handle(CostRouteQuery req, CancellationToken ct)
    {
        var today = req.On ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var onFile = req.DieselPricePerLitre is null
            ? await tenantUow.Repository<DieselPrice>().Query()
                .Where(d => d.EffectiveFrom <= today)
                .OrderByDescending(d => d.EffectiveFrom)
                .FirstOrDefaultAsync(ct)
            : null;

        var diesel = req.DieselPricePerLitre ?? onFile?.PricePerLitre;
        if (diesel is null)
        {
            return Result<RouteCostingDto>.Fail("No diesel price is set. Enter one, or set the current diesel price.");
        }

        var defaults = RouteCosting.DefaultsFor(req.TruckSize, req.SeniorDriver);
        var assumptions = new RouteCostingAssumptions(
            req.KmPerLitre ?? defaults.KmPerLitre,
            req.DriverDayRate ?? defaults.DriverDayRate,
            req.HelperDayRate ?? defaults.HelperDayRate,
            req.OtherCrewCost ?? defaults.OtherCrewCost,
            req.DriverAllowance ?? defaults.DriverAllowance,
            req.HelperAllowance ?? defaults.HelperAllowance,
            req.AdminPercent ?? defaults.AdminPercent,
            req.MarkupPercent ?? defaults.MarkupPercent,
            req.Buffer ?? defaults.Buffer,
            req.RoundUpTo ?? defaults.RoundUpTo);

        try
        {
            var result = RouteCosting.Compute(
                req.GarageToWarehouseKm, req.WarehouseToDropKm, req.DropToGarageKm, diesel.Value, assumptions);

            return Result<RouteCostingDto>.Ok(new RouteCostingDto
            {
                TruckSize = req.TruckSize,
                DieselPricePerLitre = diesel.Value,
                DieselPriceFrom = onFile?.EffectiveFrom,
                KmPerLitre = assumptions.KmPerLitre,
                DriverDayRate = assumptions.DriverDayRate,
                HelperDayRate = assumptions.HelperDayRate,
                OtherCrewCost = assumptions.OtherCrewCost,
                DriverAllowance = assumptions.DriverAllowance,
                HelperAllowance = assumptions.HelperAllowance,
                AdminPercent = assumptions.AdminPercent,
                MarkupPercent = assumptions.MarkupPercent,
                Buffer = assumptions.Buffer,
                RoundUpTo = assumptions.RoundUpTo,
                TotalKm = result.TotalKm,
                Litres = result.Litres,
                Fuel = result.Fuel,
                Crew = result.Crew,
                Expenses = result.Expenses,
                Admin = result.Admin,
                Markup = result.Markup,
                BeforeRounding = result.BeforeRounding,
                SellingRate = result.SellingRate
            });
        }
        catch (InvalidOperationException ex)
        {
            return Result<RouteCostingDto>.Fail(ex.Message);
        }
    }
}
