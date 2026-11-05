using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Abstractions.Documents;

/// <summary>
/// Renders a customer-facing formal rate offer (quotation) as a PDF.
/// </summary>
public interface IQuotationPdfGenerator
{
    byte[] Generate(QuotationPdfRequest request);
}

/// <summary>
/// Everything the quotation PDF renders. The document layer has no database access, so the
/// names the aggregate stores as ids (customer, route locations) are resolved by the caller.
/// </summary>
/// <param name="Quotation">The quotation with its lines and their tariff rates loaded.</param>
/// <param name="Tenant">The tenant whose letterhead the offer is printed on.</param>
/// <param name="CustomerName">The customer the offer is addressed to.</param>
/// <param name="CustomerAddress">The customer's address, already formatted for print.</param>
/// <param name="LocationNames">Route location id to display name.</param>
/// <param name="LogoBytes">The tenant logo, or null to print the letterhead without one.</param>
public sealed record QuotationPdfRequest(
    Quotation Quotation,
    Tenant Tenant,
    string CustomerName,
    string? CustomerAddress,
    IReadOnlyDictionary<Guid, string> LocationNames,
    byte[]? LogoBytes);
