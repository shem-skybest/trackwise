using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public record PaymentDto
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }

    public Money Amount { get; set; } = null!;
    public string? StripePaymentMethodId { get; set; }
    public Guid TenantId { get; set; }
    public PaymentStatus Status { get; set; }
    public Address? BillingAddress { get; set; }
    public string? Description { get; set; }

    /// <summary>Online gateway that took the payment (Xendit, Stripe); null for one recorded by hand.</summary>
    public PaymentGateway? Gateway { get; set; }

    /// <summary>Gateway's own id, or the check / transfer number typed in for a manual payment.</summary>
    public string? ReferenceNumber { get; set; }
}
