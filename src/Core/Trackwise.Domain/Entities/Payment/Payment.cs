using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

public class Payment : AuditableEntity, IMasterEntity, ITenantEntity
{
    public required Money Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? StripePaymentMethodId { get; set; }
    public required Guid TenantId { get; set; }
    public string? Description { get; set; }
    public required Address BillingAddress { get; set; }
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Which online gateway took the payment. Null for manual (cash/check) payments.
    /// </summary>
    public PaymentGateway? Gateway { get; set; }

    /// <summary>
    /// The gateway's own id for the payment (Xendit invoice id). Unique per gateway, so a
    /// retried callback can't record the same payment twice.
    /// </summary>
    public string? GatewayReference { get; set; }

    #region Manual Payment Fields

    /// <summary>
    /// Reference number for manual payments (check number, receipt number, etc.).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// User ID who recorded the manual payment.
    /// </summary>
    public Guid? RecordedByUserId { get; set; }

    /// <summary>
    /// When the manual payment was recorded.
    /// </summary>
    public DateTime? RecordedAt { get; set; }

    #endregion
}
