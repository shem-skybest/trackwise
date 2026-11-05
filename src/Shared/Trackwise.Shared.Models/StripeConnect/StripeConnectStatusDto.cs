using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

public record StripeConnectStatusDto
{
    public string? AccountId { get; set; }
    public StripeConnectStatus Status { get; set; }
    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public bool IsOnboarded => Status == StripeConnectStatus.Active;
}
