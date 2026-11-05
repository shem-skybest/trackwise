using Trackwise.Application.Abstractions.Documents;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Documents.Pdf.Shared;
using Trackwise.Shared.Models;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestDocument = QuestPDF.Fluent.Document;

namespace Trackwise.Infrastructure.Documents.Pdf.Billing;

public class BillingBatchPdfService : IBillingBatchPdfService
{
    public byte[] GenerateBillingBatchPdf(BillingBatchDto batch, Tenant tenant, byte[]? logo, string preparedBy)
    {
        Settings.License = LicenseType.Community;
        var logoImage = PdfLogo.TryDecode(logo);

        var doc = QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(8.5f));

                page.Header().Element(c => BillingBatchHeaderSection.Render(c, batch, tenant, logoImage));

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(12);

                    if (batch.Status is BillingBatchStatus.Draft)
                    {
                        col.Item()
                            .Background(Colors.Orange.Lighten4)
                            .Padding(6)
                            .Text("DRAFT - not locked yet. The amounts can still change; lock the batch before forwarding it to Accounting.")
                            .FontSize(8).FontColor(Colors.Orange.Darken4);
                    }

                    col.Item().Element(c => BillingBatchTripsSection.Render(c, batch));

                    if (batch.Charges.Count > 0)
                    {
                        col.Item().Element(c => BillingBatchChargesSection.Render(c, batch));
                    }

                    col.Item().Element(c => BillingBatchTotalsSection.Render(c, batch));

                    if (!string.IsNullOrWhiteSpace(batch.Notes))
                    {
                        col.Item().Text($"Remarks: {batch.Notes}").FontSize(8);
                    }

                    col.Item().PaddingTop(18).Element(c => BillingBatchSignOffSection.Render(c, preparedBy));
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"{batch.ReferenceNumber} · printed {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(7).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken1));
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }
}
