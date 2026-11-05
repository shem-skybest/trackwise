using Trackwise.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Trackwise.Infrastructure.Documents.Pdf.Billing;

/// <summary>The figures Accounting keys into SAS: nSFS's Selling, Service and Non-VAT columns.</summary>
internal static class BillingBatchTotalsSection
{
    public static void Render(IContainer container, BillingBatchDto batch)
    {
        var tripRates = batch.Trips.Sum(t => t.Amount ?? 0);
        var vatableCharges = batch.Charges.Where(c => c.IsVatable).Sum(c => c.Amount);

        container.AlignRight().Width(240).Column(col =>
        {
            Line(col, "Trip rates", tripRates);
            if (vatableCharges > 0)
            {
                Line(col, "VATable charges", vatableCharges);
            }

            Line(col, "Service (VATable)", batch.VatableTotal, bold: true);
            Line(col, "Non-VAT (at cost)", batch.NonVatTotal);

            col.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text("Selling total").FontSize(10).Bold();
                row.ConstantItem(110).AlignRight()
                    .Text($"{batch.Currency} {BillingBatchTripsSection.Amount(batch.Total)}").FontSize(10).Bold();
            });
        });
    }

    private static void Line(ColumnDescriptor col, string label, decimal amount, bool bold = false) =>
        col.Item().PaddingVertical(1.5f).Row(row =>
        {
            var text = row.RelativeItem().Text(label);
            var value = row.ConstantItem(110).AlignRight().Text(BillingBatchTripsSection.Amount(amount));
            if (bold)
            {
                text.SemiBold();
                value.SemiBold();
            }
        });
}
