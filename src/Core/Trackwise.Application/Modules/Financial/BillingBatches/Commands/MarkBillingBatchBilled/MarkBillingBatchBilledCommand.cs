using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>Records SAS's invoice number once Accounting has billed the batch.</summary>
public class MarkBillingBatchBilledCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }

    /// <summary>As Accounting gives it, e.g. "BI4907 / BS7259".</summary>
    public string InvoiceNumber { get; set; } = null!;

    /// <summary>Defaults to today.</summary>
    public DateOnly? BilledOn { get; set; }
}
