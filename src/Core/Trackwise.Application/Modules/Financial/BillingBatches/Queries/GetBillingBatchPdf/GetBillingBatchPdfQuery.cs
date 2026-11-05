using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

/// <summary>The printed batch summary Ops hands Accounting, signed off below the totals.</summary>
public class GetBillingBatchPdfQuery : IQuery<Result<BillingBatchPdf>>
{
    public Guid Id { get; set; }
}

public sealed record BillingBatchPdf(string ReferenceNumber, byte[] Content);
