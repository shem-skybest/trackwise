using Trackwise.Application.Abstractions.Payments.Stripe;
namespace Trackwise.Application.Abstractions.Payments.Stripe;

public interface IStripeService
{
    /// <summary>
    ///     Stripe webhook secret
    /// </summary>
    string WebhookSecret { get; }
}
