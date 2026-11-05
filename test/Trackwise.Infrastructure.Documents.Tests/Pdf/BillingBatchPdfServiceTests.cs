using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Infrastructure.Documents.Pdf.Billing;
using Trackwise.Shared.Models;
using UglyToad.PdfPig;
using Xunit;

namespace Trackwise.Infrastructure.Documents.Tests.Pdf;

public class BillingBatchPdfServiceTests
{
    // 1x1 opaque PNG
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private readonly BillingBatchPdfService sut = new();

    [Fact]
    public void GenerateBillingBatchPdf_WithLogo_DrawsItOnTheLetterhead()
    {
        var pdf = sut.GenerateBillingBatchPdf(Batch(), Tenant(), Png, "Shem de Guzman");

        Assert.Equal(1, ImageCount(pdf));
    }

    [Fact]
    public void GenerateBillingBatchPdf_WithoutLogo_PrintsTheLetterheadAlone()
    {
        var pdf = sut.GenerateBillingBatchPdf(Batch(), Tenant(), null, "Shem de Guzman");

        Assert.Equal(0, ImageCount(pdf));
    }

    [Fact]
    public void GenerateBillingBatchPdf_WithUnreadableLogo_StillPrints()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray();

        var pdf = sut.GenerateBillingBatchPdf(Batch(), Tenant(), svg, "Shem de Guzman");

        Assert.Equal(0, ImageCount(pdf));
    }

    private static int ImageCount(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return document.GetPages().Sum(page => page.GetImages().Count());
    }

    private static BillingBatchDto Batch() => new()
    {
        ReferenceNumber = "SLC-RFV0001-26-M",
        CustomerName = "Hacienda Verde Inc.",
        PeriodStart = new DateOnly(2026, 9, 16),
        PeriodEnd = new DateOnly(2026, 9, 30),
        Currency = "PHP",
        Status = BillingBatchStatus.Billed,
    };

    private static Tenant Tenant() => new()
    {
        Name = "skybest",
        CompanyName = "Skybest Logistics Corporation",
        ConnectionString = "",
        BillingEmail = "billing@skybest.com.ph",
        CompanyAddress = new Address
        {
            StreetAddress = "1 Test St",
            CityOrMunicipality = "Pasay",
            Province = "Metro Manila",
            Region = "NCR",
            Zipcode = "1300",
            Country = "PH",
        },
    };
}
