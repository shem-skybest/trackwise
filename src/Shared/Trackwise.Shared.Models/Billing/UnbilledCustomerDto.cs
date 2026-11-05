using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>A client's completed trips not on any billing batch yet, with the period its oldest one falls in.</summary>
public record UnbilledCustomerDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public BillingCycle? BillingCycle { get; set; }
    public int TripCount { get; set; }
    public decimal PricedTotal { get; set; }

    /// <summary>Trips that need pricing before they can be billed.</summary>
    public int UnpricedCount { get; set; }

    public DateOnly OldestDate { get; set; }

    /// <summary>The billing period the oldest trip falls in, from the client's cycle; null without one.</summary>
    public DateOnly? SuggestedPeriodStart { get; set; }

    public DateOnly? SuggestedPeriodEnd { get; set; }
    public IReadOnlyList<BillingBatchTripDto> Trips { get; set; } = [];
}
