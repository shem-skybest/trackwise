using Trackwise.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Trackwise.Infrastructure.Documents.Pdf.Billing;

internal static class BillingBatchChargesSection
{
    public static void Render(IContainer container, BillingBatchDto batch)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(4).Text("Other charges").FontSize(9).SemiBold();
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);   // Description
                    columns.ConstantColumn(40);  // Trip
                    columns.ConstantColumn(50);  // VAT
                    columns.ConstantColumn(62);  // Amount
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell()).Text("Description");
                    HeaderCell(header.Cell()).Text("Trip");
                    HeaderCell(header.Cell()).Text("VAT");
                    HeaderCell(header.Cell()).AlignRight().Text("Amount");
                });

                foreach (var charge in batch.Charges)
                {
                    BodyCell(table.Cell()).Text(charge.Description);
                    BodyCell(table.Cell()).Text(charge.TripNumber?.ToString() ?? "-");
                    BodyCell(table.Cell()).Text(charge.IsVatable ? "VATable" : "Non-VAT");
                    BodyCell(table.Cell()).AlignRight().Text(BillingBatchTripsSection.Amount(charge.Amount));
                }
            });
        });
    }

    private static IContainer HeaderCell(IContainer cell) =>
        cell.BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).PaddingHorizontal(2)
            .DefaultTextStyle(x => x.FontSize(7.5f).Bold());

    private static IContainer BodyCell(IContainer cell) =>
        cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(2);
}
