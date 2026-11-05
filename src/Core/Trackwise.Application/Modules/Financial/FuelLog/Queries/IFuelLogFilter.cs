using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Financial.FuelLog.Queries;

/// <summary>The filters the fill-up list and its summary share, so the totals always match the rows.</summary>
public interface IFuelLogFilter
{
    Guid? TruckId { get; }
    FleetCard? FleetCard { get; }
    ExpenseStatus? Status { get; }

    /// <summary>First day, in the tenant's time zone.</summary>
    DateOnly? From { get; }

    /// <summary>Last day, included, in the tenant's time zone.</summary>
    DateOnly? To { get; }
}

internal static class FuelLogFilterExtensions
{
    public static IQueryable<TruckExpense> Apply(
        this IQueryable<TruckExpense> query, IFuelLogFilter filter, TenantSettings settings)
    {
        if (filter.TruckId is { } truckId)
        {
            query = query.Where(e => e.TruckId == truckId);
        }

        if (filter.FleetCard is { } card)
        {
            query = query.Where(e => e.FleetCard == card);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(e => e.Status == status);
        }

        if (filter.From is { } from)
        {
            var start = settings.ToUtc(from, TimeOnly.MinValue);
            query = query.Where(e => e.ExpenseDate >= start);
        }

        if (filter.To is { } to)
        {
            var end = settings.ToUtc(to.AddDays(1), TimeOnly.MinValue);
            query = query.Where(e => e.ExpenseDate < end);
        }

        return query;
    }
}
