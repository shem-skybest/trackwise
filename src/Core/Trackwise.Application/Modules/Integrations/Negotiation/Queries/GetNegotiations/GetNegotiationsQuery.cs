using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Queries;

[RequiresFeature(TenantFeature.AIRateNegotiation)]
public class GetNegotiationsQuery : PagedQuery, IQuery<PagedResult<RateNegotiationDto>>
{
    public RateNegotiationStatus? Status { get; set; }

    /// <summary>Only threads still waiting on the broker or already answered by them.</summary>
    public bool ActiveOnly { get; set; }
}
