using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Webhooks.Commands;

/// <summary>
/// A Xendit "Invoices paid" callback. The tenant comes from the callback URL, which each company
/// sets in its own Xendit dashboard (<c>/webhooks/xendit/{tenantId}</c>).
/// </summary>
public class ProcessXenditCallbackCommand : ICommand<Result>
{
    public required Guid TenantId { get; set; }
    public required string RawBody { get; set; }

    /// <summary>The <c>x-callback-token</c> header.</summary>
    public string? CallbackToken { get; set; }
}
