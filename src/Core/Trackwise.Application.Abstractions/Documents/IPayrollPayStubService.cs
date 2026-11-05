using Trackwise.Domain.Entities;
using Trackwise.Application.Abstractions.Documents;

namespace Trackwise.Application.Abstractions.Documents;

/// <summary>
/// Service for generating payroll pay stub PDF documents.
/// </summary>
public interface IPayrollPayStubService
{
    /// <summary>
    /// Generates a PDF pay stub for a payroll invoice.
    /// </summary>
    /// <param name="payroll">The payroll invoice entity with related data loaded.</param>
    /// <param name="tenant">The tenant for company information.</param>
    /// <param name="logo">The tenant logo image, or null to print the letterhead without one.</param>
    /// <returns>PDF file as byte array.</returns>
    byte[] GeneratePayStubPdf(PayrollInvoice payroll, Tenant tenant, byte[]? logo);
}
