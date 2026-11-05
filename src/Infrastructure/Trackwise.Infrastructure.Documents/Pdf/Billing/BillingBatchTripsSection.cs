using System.Globalization;
using Trackwise.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Trackwise.Infrastructure.Documents.Pdf.Billing;

internal static class BillingBatchTripsSection
{
    public static void Render(IContainer container, BillingBatchDto batch)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(18);   // #
                columns.ConstantColumn(44);   // Date
                columns.ConstantColumn(34);   // Trip
                columns.RelativeColumn(2);    // Booking
                columns.ConstantColumn(50);   // Plate
                columns.ConstantColumn(28);   // Drops
                columns.RelativeColumn(2);    // Outcome
                columns.RelativeColumn(2);    // Rate
                columns.ConstantColumn(62);   // Amount
            });

            table.Header(header =>
            {
                foreach (var title in new[] { "#", "Date", "Trip", "Booking", "Plate", "Drops", "Outcome", "Rate", "Amount" })
                {
                    var cell = HeaderCell(header.Cell());
                    if (title is "Drops" or "Amount")
                    {
                        cell.AlignRight().Text(title);
                    }
                    else
                    {
                        cell.Text(title);
                    }
                }
            });

            var index = 1;
            foreach (var trip in batch.Trips)
            {
                BodyCell(table.Cell()).Text(index++.ToString(CultureInfo.InvariantCulture));
                BodyCell(table.Cell()).Text(trip.BillingDate.ToString("MM/dd", CultureInfo.InvariantCulture));
                BodyCell(table.Cell()).Text(trip.TripNumber.ToString(CultureInfo.InvariantCulture));
                BodyCell(table.Cell()).Text(trip.BookingReferences ?? "-");
                BodyCell(table.Cell()).Text(trip.TruckNumber ?? "-");
                BodyCell(table.Cell()).AlignRight().Text(trip.Drops.ToString(CultureInfo.InvariantCulture));
                BodyCell(table.Cell()).Text(Outcome(trip));
                BodyCell(table.Cell()).Text(trip.RateLineLabel ?? "-");
                BodyCell(table.Cell()).AlignRight().Text(trip.Amount is { } amount ? Amount(amount) : "not priced");
            }
        });
    }

    internal static string Amount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

    private static string Outcome(BillingBatchTripDto trip)
    {
        var parts = new List<string>();
        if (trip.FoulTripDrops > 0)
        {
            parts.Add($"{trip.FoulTripDrops} foul trip");
        }

        if (trip.CancelledDrops > 0)
        {
            parts.Add($"{trip.CancelledDrops} cancelled by client");
        }

        if (trip.PartialDrops > 0)
        {
            parts.Add($"{trip.PartialDrops} partial");
        }

        return parts.Count == 0 ? "Delivered" : string.Join(", ", parts);
    }

    private static IContainer HeaderCell(IContainer cell) =>
        cell.BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).PaddingHorizontal(2)
            .DefaultTextStyle(x => x.FontSize(7.5f).Bold());

    private static IContainer BodyCell(IContainer cell) =>
        cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(2);
}
