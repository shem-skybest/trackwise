using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Queries;

/// <summary>A period's fill-ups added up: overall, per fleet card and per truck.</summary>
public class GetFuelSummaryQuery : IQuery<Result<FuelSummaryDto>>, IFuelLogFilter
{
    public Guid? TruckId { get; set; }
    public FleetCard? FleetCard { get; set; }
    public ExpenseStatus? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
