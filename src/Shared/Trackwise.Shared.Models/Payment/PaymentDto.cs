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
}
