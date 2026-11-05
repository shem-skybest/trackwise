using Trackwise.Domain.Entities;
using Trackwise.Infrastructure.Documents.Pdf.Payroll;
using Trackwise.Infrastructure.Documents.Pdf.Shared;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Trackwise.Application.Abstractions.Documents;
using QuestDocument = QuestPDF.Fluent.Document;

namespace Trackwise.Infrastructure.Documents.Pdf;

public class PayrollPayStubService : IPayrollPayStubService
{
    public byte[] GeneratePayStubPdf(PayrollInvoice payroll, Tenant tenant, byte[]? logo)
    {
        Settings.License = LicenseType.Community;
        var logoImage = PdfLogo.TryDecode(logo);

        var doc = QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => PayrollHeaderSection.Render(c, payroll, tenant, logoImage));
                page.Content().PaddingVertical(20).Element(c => PayrollEarningsSection.Render(c, payroll));
                page.Footer().Element(c => PdfFooter.Render(c, tenant, "This is a pay stub for your records."));
            });
        });

        return doc.GeneratePdf();
    }
}
