using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trucks.Queries;

public class GetTrucksQuery : SearchableQuery, IQuery<PagedResult<TruckDto>>
{
    public bool IncludeLoads { get; set; } = false;
    public TruckStatus[]? Statuses { get; set; }
    public TruckType[]? Types { get; set; }
}
