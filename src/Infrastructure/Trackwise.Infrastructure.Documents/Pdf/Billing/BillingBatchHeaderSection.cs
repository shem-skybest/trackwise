using Trackwise.Domain.Entities;
using Trackwise.Infrastructure.Documents.Pdf.Shared;
using Trackwise.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Trackwise.Infrastructure.Documents.Pdf.Billing;

internal static class BillingBatchHeaderSection
{
    public static void Render(IContainer container, BillingBatchDto batch, Tenant tenant, Image? logo)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (logo is not null)
                {
                    row.AutoItem().PaddingRight(10).Element(c => PdfLogo.Render(c, logo, height: 34, maxWidth: 120));
                }

                row.RelativeItem().AlignMiddle().Column(left =>
                {
                    left.Item().Text((tenant.CompanyName ?? tenant.Name).ToUpperInvariant()).FontSize(12).Bold();
                    left.Item().Text("BILLING SUMMARY").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(200).Column(right =>
                {
                    right.Item().AlignRight().Text(batch.ReferenceNumber).FontSize(13).Bold();
                    right.Item().AlignRight().Text(batch.Status.ToString().ToUpperInvariant())
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(6).Row(row =>
            {
                Field(row.RelativeItem(2), "Client", batch.CustomerName ?? "-");
                Field(row.RelativeItem(2), "Billing period", Period(batch.PeriodStart, batch.PeriodEnd));
                Field(row.RelativeItem(), "Trips", batch.TripCount.ToString());
                Field(row.RelativeItem(), "Drops", batch.DropCount.ToString());
            });
        });
    }

    private static string Period(DateOnly start, DateOnly end) =>
        start == end ? $"{start:MMM d, yyyy}" : $"{start:MMM d, yyyy} - {end:MMM d, yyyy}";

    private static void Field(IContainer container, string label, string value) =>
        container.Column(c =>
        {
            c.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken1);
            c.Item().Text(value).FontSize(9).SemiBold();
        });
}
