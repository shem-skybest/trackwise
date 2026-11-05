using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.Loads.Specifications;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Extensions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Platform.Stats.Queries;

internal sealed class GetDailyGrossesHandler : IAppRequestHandler<GetDailyGrossesQuery, Result<DailyGrossesDto>>
{
    private readonly ITenantUnitOfWork _tenantUow;

    public GetDailyGrossesHandler(ITenantUnitOfWork tenantUow)
    {
        _tenantUow = tenantUow;
    }

    public async Task<Result<DailyGrossesDto>> Handle(
        GetDailyGrossesQuery req, CancellationToken ct)
    {
        var truckId = req.TruckId;

        if (req.UserId.HasValue)
        {
            // Past the soft-delete filter: a driver's earnings history must keep resolving through
            // the truck they were assigned to even after that truck has since been retired.
            var truck = await _tenantUow.Repository<Truck>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.MainDriverId == req.UserId.Value ||
                                          i.SecondaryDriverId == req.UserId.Value, ct);

            if (truck is null)
            {
                return Result<DailyGrossesDto>.Fail($"Could not find a truck with driver ID '{req.UserId}'");
            }

            truckId = truck.Id;
        }

        // Ensure dates are UTC for PostgreSQL compatibility
        var startDate = DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(req.EndDate, DateTimeKind.Utc);

        var spec = new FilterLoadsByDeliveryDate(truckId, startDate, endDate);

        var days = req.StartDate.DaysBetween(req.EndDate);
        var dict = days.ToDictionary(
            k => (k.Year, k.Month, k.Day),
            m => new DailyGrossDto(m.Year, m.Month, m.Day));

        var filteredLoads = _tenantUow.Repository<Load>().ApplySpecification(spec).ToArray();

        foreach (var load in filteredLoads)
        {
            var date = load.DeliveredAt!.Value;
            var key = (date.Year, date.Month, date.Day);

            if (!dict.ContainsKey(key))
            {
                continue;
            }

            dict[key].Gross += load.DeliveryCost;
            dict[key].Distance += load.Distance;
            dict[key].DriverShare += load.CalcDriverShare();
        }

        var dailyGrosses = new DailyGrossesDto
        {
            Data = dict.Values
        };
        return Result<DailyGrossesDto>.Ok(dailyGrosses);
    }
}
