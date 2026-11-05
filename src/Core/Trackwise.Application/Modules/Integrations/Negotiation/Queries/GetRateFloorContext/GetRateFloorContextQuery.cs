using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Queries;

[RequiresFeature(TenantFeature.AIRateNegotiation)]
public class GetRateFloorContextQuery : IQuery<Result<RateFloorContextDto>>
{
    public Guid ListingId { get; set; }
}
