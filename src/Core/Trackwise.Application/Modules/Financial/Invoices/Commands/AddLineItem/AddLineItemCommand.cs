using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

/// <summary>
/// Adds a line item to an invoice.
/// </summary>
public record AddLineItemCommand : ICommand<Result<InvoiceLineItemDto>>
{
    public required Guid InvoiceId { get; set; }
    public required string Description { get; set; }
    public required InvoiceLineItemType Type { get; set; }
    public required decimal Amount { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
}
