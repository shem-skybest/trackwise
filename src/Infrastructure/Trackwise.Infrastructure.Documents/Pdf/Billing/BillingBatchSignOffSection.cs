using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Trackwise.Infrastructure.Documents.Pdf.Billing;

/// <summary>The signatures nSFS's summary carried: who prepared it, who noted it, and Accounting receiving it.</summary>
internal static class BillingBatchSignOffSection
{
    public static void Render(IContainer container, string preparedBy)
    {
        container.Row(row =>
        {
            row.Spacing(24);
            Signature(row.RelativeItem(), "Prepared by:", preparedBy.ToUpperInvariant());
            Signature(row.RelativeItem(), "Noted by:", null);
            Signature(row.RelativeItem(), "Received by:", "ACCOUNTING");
        });
    }

    private static void Signature(IContainer container, string label, string? name) =>
        container.Column(col =>
        {
            col.Item().Text(label).FontSize(8);
            col.Item().PaddingTop(22).BorderBottom(0.75f).BorderColor(Colors.Black).Height(1);
            col.Item().PaddingTop(2).Text(name ?? " ").FontSize(8).SemiBold();
            col.Item().Text("Date: ______________").FontSize(7).FontColor(Colors.Grey.Darken1);
        });
}
