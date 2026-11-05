using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Queries;

[RequiresFeature(TenantFeature.FuelCards)]
public class GetFuelCardTransactionsQuery : PagedQuery, IQuery<PagedResult<FuelCardTransactionDto>>
{
    public FuelCardTransactionStatus? Status { get; set; }
    public FuelCardProviderType? ProviderType { get; set; }
    public Guid? TruckId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
