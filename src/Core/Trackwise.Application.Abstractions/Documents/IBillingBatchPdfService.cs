using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Abstractions.Documents;

/// <summary>
/// Renders a billing batch as the summary Ops prints for Accounting - nSFS's "Billing Summary -
/// Reefer", with the trips behind the totals and the sign-off lines.
/// </summary>
public interface IBillingBatchPdfService
{
    byte[] GenerateBillingBatchPdf(BillingBatchDto batch, Tenant tenant, byte[]? logo, string preparedBy);
}
