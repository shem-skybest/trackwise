using Trackwise.Domain.Entities;

namespace Trackwise.Application.Abstractions.Documents;

/// <summary>
/// Port for rendering a Booking as a PDF. The implementation lives in Infrastructure
/// (QuestPDF is an infrastructure dependency); Application only knows this interface.
/// </summary>
public interface IBookingPdfGenerator
{
    byte[] Generate(BookingPdfRequest request);
}

/// <summary>
/// Everything the booking PDF prints. The booking must be loaded with Charges and Documents;
/// names that live outside the aggregate are resolved by the caller in batched lookups.
/// </summary>
/// <param name="LocationNames">LocationId -> name for every stop on the booking's lane.</param>
/// <param name="IncludeInternalCosts">
/// False (default) produces the customer-facing document. True adds buying cost and margin and
/// stamps the page "INTERNAL COPY" - only ever offer it to users allowed to see costs.
/// </param>
/// <param name="UtcOffset">Time zone used to print dates and windows. Defaults to UTC+8.</param>
public sealed record BookingPdfRequest(
    Booking Booking,
    Tenant Tenant,
    string CustomerName,
    string? CustomerAddress,
    IReadOnlyDictionary<Guid, string> LocationNames,
    byte[]? LogoBytes = null,
    bool IncludeInternalCosts = false,
    TimeSpan? UtcOffset = null);
