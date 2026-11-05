using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.StripeConnect.Commands;

/// <summary>
/// Creates a Stripe Connect Express account for the current tenant.
/// </summary>
public record CreateConnectAccountCommand : ICommand<Result<CreateConnectAccountDto>>;
