using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

internal sealed class GetUnbilledTripsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetUnbilledTripsQuery, Result<IReadOnlyList<UnbilledCustomerDto>>>
{
    public async Task<Result<IReadOnlyList<UnbilledCustomerDto>>> Handle(GetUnbilledTripsQuery req, CancellationToken ct)
    {
        var trips = await BillableTripReader.UnbilledAsync(tenantUow, ct);

        var customerIds = trips.Select(t => t.CustomerId).Distinct().ToList();
        var customers = await tenantUow.Repository<Customer>().Query()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.BillingCycle })
            .ToDictionaryAsync(c => c.Id, ct);

        var result = trips
            .Where(t => customers.ContainsKey(t.CustomerId))
            .GroupBy(t => t.CustomerId)
            .Select(g =>
            {
                var customer = customers[g.Key];
                var ordered = g.OrderBy(t => t.BillingDate).ThenBy(t => t.TripNumber).ToList();
                var oldest = ordered[0].BillingDate;
                BillingPeriod? suggested = customer.BillingCycle is { } cycle ? BillingPeriod.For(cycle, oldest) : null;

                return new UnbilledCustomerDto
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    BillingCycle = customer.BillingCycle,
                    TripCount = ordered.Count,
                    PricedTotal = ordered.Sum(t => t.Amount ?? 0),
                    UnpricedCount = ordered.Count(t => t.Amount is null),
                    OldestDate = oldest,
                    SuggestedPeriodStart = suggested?.Start,
                    SuggestedPeriodEnd = suggested?.End,
                    Trips = ordered.Select(t => t.ToDto()).ToList()
                };
            })
            .OrderBy(c => c.OldestDate)
            .ThenBy(c => c.CustomerName)
            .ToList();

        return Result<IReadOnlyList<UnbilledCustomerDto>>.Ok(result);
    }
}
