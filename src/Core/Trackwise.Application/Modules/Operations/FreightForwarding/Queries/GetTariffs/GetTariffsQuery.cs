// ─────────────────────────────────────────────
// GetTariffsQuery.cs
// ─────────────────────────────────────────────
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public class GetTariffsQuery : IQuery<PagedResult<TariffDto>>
{
    public bool OnlyActive { get; set; } = true;
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
    public FreightMode? Mode { get; set; }
    public ServiceLevel? ServiceLevel { get; set; }
    public Guid? CarrierId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? OriginLane { get; set; }
    public string? DestinationLane { get; set; }
    public DateOnly? EffectiveOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
