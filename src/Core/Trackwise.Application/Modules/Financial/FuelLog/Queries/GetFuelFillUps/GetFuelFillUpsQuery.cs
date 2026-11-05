using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Queries;

/// <summary>Fill-ups, latest first.</summary>
public class GetFuelFillUpsQuery : PagedQuery, IQuery<PagedResult<FuelFillUpDto>>, IFuelLogFilter
{
    public Guid? TruckId { get; set; }
    public FleetCard? FleetCard { get; set; }
    public ExpenseStatus? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
