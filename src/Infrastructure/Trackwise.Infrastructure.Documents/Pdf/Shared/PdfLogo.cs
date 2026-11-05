using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Trackwise.Infrastructure.Documents.Pdf.Shared;

/// <summary>
/// The tenant logo on a letterhead. Decoding happens up front so a file Skia can't read (an SVG,
/// a corrupt upload) drops the logo instead of throwing halfway through rendering the document.
/// </summary>
internal static class PdfLogo
{
    public static Image? TryDecode(byte[]? logo)
    {
        if (logo is not { Length: > 0 })
        {
            return null;
        }

        try
        {
            return Image.FromBinaryData(logo);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Scales the logo down to fit a <paramref name="maxWidth"/> x <paramref name="height"/> box, left-aligned.</summary>
    public static void Render(IContainer container, Image logo, float height, float maxWidth) =>
        container.Height(height).MaxWidth(maxWidth).AlignLeft().AlignMiddle().Image(logo).FitArea();
}
