using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Queries;

/// <summary>
/// Renders the email a pending counter-offer decision would send, so approving it is a decision
/// about the real message rather than about a summary of it.
/// </summary>
[RequiresFeature(TenantFeature.AIRateNegotiation)]
public class PreviewCounterOfferQuery : IQuery<Result<CounterOfferPreviewDto>>
{
    public Guid DecisionId { get; set; }
}
