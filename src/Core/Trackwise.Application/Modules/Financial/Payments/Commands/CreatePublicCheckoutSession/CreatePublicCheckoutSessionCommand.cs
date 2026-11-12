using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

/// <summary>
/// Starts a hosted payment for a public invoice payment link and returns the page URL the
/// customer is redirected to. A tenant with an active Xendit account gets a Xendit invoice
/// (settled by the Xendit invoice callback); otherwise a Stripe Checkout Session (settled by the
/// <c>checkout.session.completed</c> webhook).
/// </summary>
public record CreatePublicCheckoutSessionCommand : ICommand<Result<PublicCheckoutSessionDto>>
{
    public required Guid TenantId { get; init; }
    public required string Token { get; init; }

    /// <summary>URL the gateway redirects to on successful payment.</summary>
    public required string SuccessUrl { get; init; }

    /// <summary>URL the gateway redirects to when the customer cancels or the payment fails.</summary>
    public required string CancelUrl { get; init; }

    /// <summary>Amount to pay. If null, the full <c>amountDue</c> is charged.</summary>
    public decimal? Amount { get; init; }
}
