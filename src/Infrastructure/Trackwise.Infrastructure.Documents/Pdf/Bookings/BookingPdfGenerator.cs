using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Trackwise.Application.Abstractions.Documents;

namespace Trackwise.Infrastructure.Documents.Pdf.Bookings;

/// <summary>
/// Renders a Booking as a PDF. Lives in the infrastructure document layer because QuestPDF is an
/// infrastructure dependency - the Application layer only sees <see cref="IBookingPdfGenerator"/>.
/// </summary>
public sealed class BookingPdfGenerator : IBookingPdfGenerator
{
    public byte[] Generate(BookingPdfRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Settings.License = LicenseType.Community;
        var document = new BookingPdfDocument(request);
        return document.GeneratePdf();
    }
}
