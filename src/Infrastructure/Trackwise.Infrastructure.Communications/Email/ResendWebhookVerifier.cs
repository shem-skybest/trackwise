using Trackwise.Application.Abstractions.Email;
using Trackwise.Infrastructure.Integrations.Common;
using Microsoft.Extensions.Options;

namespace Trackwise.Infrastructure.Communications.Email;

/// <summary>Resend signs webhooks with Svix.</summary>
internal sealed class ResendWebhookVerifier(IOptions<ResendOptions> options) : IInboundEmailWebhookVerifier
{
    public bool Verify(string rawBody, string? messageId, string? timestamp, string? signature) =>
        WebhookSignature.VerifySvix(rawBody, messageId, timestamp, signature, options.Value.WebhookSecret);
}
