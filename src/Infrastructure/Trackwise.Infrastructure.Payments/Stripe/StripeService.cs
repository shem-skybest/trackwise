using Microsoft.Extensions.Options;
using Trackwise.Application.Abstractions.Payments.Stripe;

namespace Trackwise.Infrastructure.Payments.Stripe;

internal sealed class StripeService(IOptions<StripeOptions> options) : IStripeService
{
    public string WebhookSecret { get; } = options.Value.WebhookSecret ??
                                           throw new ArgumentNullException(nameof(options.Value.WebhookSecret));
}
