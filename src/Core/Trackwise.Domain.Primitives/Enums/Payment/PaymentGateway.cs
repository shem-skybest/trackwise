using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// The online payment processor a customer paid through.
/// </summary>
public enum PaymentGateway
{
    [Description("Stripe")]
    Stripe = 1,

    [Description("Xendit")]
    Xendit = 2
}
