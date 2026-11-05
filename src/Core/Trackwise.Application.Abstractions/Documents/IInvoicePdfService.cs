using Trackwise.Domain.Entities;
using Trackwise.Application.Abstractions.Documents;

namespace Trackwise.Application.Abstractions.Documents;

/// <summary>
/// Service for generating invoice PDF documents.
/// </summary>
public interface IInvoicePdfService
{
    /// <summary>
    /// Generates a PDF for a load invoice.
    /// </summary>
    /// <param name="invoice">The invoice entity with related data loaded.</param>
    /// <param name="tenant">The tenant for company information.</param>
    /// <param name="logo">The tenant logo image, or null to print the letterhead without one.</param>
    /// <returns>PDF file as byte array.</returns>
    byte[] GenerateLoadInvoicePdf(LoadInvoice invoice, Tenant tenant, byte[]? logo);
}
