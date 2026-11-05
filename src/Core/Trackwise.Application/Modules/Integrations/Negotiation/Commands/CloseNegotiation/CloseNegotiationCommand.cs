using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Commands;

/// <summary>
/// Ends a negotiation thread and revokes its reply address, so later broker mail to that address
/// is dropped instead of reopening the conversation.
/// </summary>
[RequiresFeature(TenantFeature.AIRateNegotiation)]
public class CloseNegotiationCommand : ICommand<Result>
{
    public Guid Id { get; set; }

    public string? Reason { get; set; }

    /// <summary>Close as declined rather than simply closed.</summary>
    public bool Declined { get; set; }
}
