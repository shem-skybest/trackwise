using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

/// <summary>
/// Removes a line item from an invoice.
/// </summary>
public record DeleteLineItemCommand(Guid InvoiceId, Guid LineItemId) : ICommand<Result>;
