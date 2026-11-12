using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The tenant's own account with an online payment gateway, used to collect invoice payments
/// straight into the company's bank account (no platform in the middle). One row per gateway.
/// Stripe is not configured here - it goes through Stripe Connect on the tenant record.
/// </summary>
public class PaymentGatewayConfiguration : Entity, ITenantEntity
{
    public required PaymentGateway Gateway { get; set; }

    /// <summary>
    /// The gateway's secret API key (Xendit: <c>xnd_production_...</c> / <c>xnd_development_...</c>).
    /// Encrypted at rest.
    /// </summary>
    [EncryptedSecret]
    public required string SecretKey { get; set; }

    /// <summary>
    /// Token the gateway sends with every callback so we can tell it is genuine
    /// (Xendit: the "Callback verification token" from the dashboard). Encrypted at rest.
    /// </summary>
    [EncryptedSecret]
    public required string CallbackToken { get; set; }

    /// <summary>
    /// Whether payment links use this gateway. Off keeps the keys but stops new payments.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>True when the key is a sandbox key (Xendit <c>xnd_development_</c>).</summary>
    public bool IsTestMode => SecretKey.StartsWith("xnd_development_", StringComparison.Ordinal);
}
