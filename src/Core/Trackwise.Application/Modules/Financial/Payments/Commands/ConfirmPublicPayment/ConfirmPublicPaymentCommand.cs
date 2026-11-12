using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

/// <summary>
/// Sent by the public pay page when the customer comes back from the hosted payment page. For a
/// Xendit company it asks Xendit directly and records anything paid, so a payment never waits on
/// (or is lost with) the "Invoices paid" callback.
/// </summary>
public record ConfirmPublicPaymentCommand : ICommand<Result<PublicPaymentConfirmationDto>>
{
    public required Guid TenantId { get; init; }
    public required string Token { get; init; }
}
