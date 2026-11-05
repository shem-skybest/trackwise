using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Trackwise.Application.Abstractions.Documents;

namespace Trackwise.Infrastructure.Documents.Pdf.Quotations;

/// <summary>
/// Renders a customer-facing formal rate offer (quotation) as a PDF. Lives in the infrastructure
/// document layer because QuestPDF is an infrastructure dependency - the Application layer
/// can't reference it, so the generation port stays in Abstractions and this is its only impl.
/// </summary>
public sealed class QuotationPdfGenerator : IQuotationPdfGenerator
{
    public byte[] Generate(QuotationPdfRequest request)
    {
        Settings.License = LicenseType.Community;
        var document = new QuotationPdfDocument(request);
        return document.GeneratePdf();
    }
}
