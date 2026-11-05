using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>
/// One email in a negotiation thread. Carries only the stripped text body - the raw provider
/// payload stays server-side.
/// </summary>
public record NegotiationMessageDto
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public NegotiationMessageDirection Direction { get; set; }
    public string? Subject { get; set; }
    public string TextBody { get; set; } = "";
    public Money? ProposedTotalRate { get; set; }
    public decimal? ProposedRatePerMile { get; set; }
    public Guid? AgentDecisionId { get; set; }
    public bool Quarantined { get; set; }
    public DateTime OccurredAt { get; set; }
}
