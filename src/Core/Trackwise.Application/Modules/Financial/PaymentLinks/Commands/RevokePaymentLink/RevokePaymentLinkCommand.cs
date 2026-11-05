using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.PaymentLinks.Commands;

/// <summary>
/// Revokes a payment link, making it invalid.
/// </summary>
public record RevokePaymentLinkCommand(Guid PaymentLinkId) : ICommand<Result>;
