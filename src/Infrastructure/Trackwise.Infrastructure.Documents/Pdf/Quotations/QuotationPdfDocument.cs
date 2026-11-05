using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Infrastructure.Documents.Pdf.Shared;

namespace Trackwise.Infrastructure.Documents.Pdf.Quotations;

/// <summary>
/// The customer-facing formal rate offer. Everything it prints comes from the quotation
/// aggregate plus the names the application layer resolved for it.
/// </summary>
public sealed class QuotationPdfDocument : IDocument
{
    private readonly Quotation _quote;
    private readonly Tenant _tenant;
    private readonly Image? _logo;
    private readonly string _customerName;
    private readonly string? _customerAddress;
    private readonly IReadOnlyDictionary<Guid, string> _locationNames;
    private readonly IReadOnlyList<PricedLine> _lines;

    private sealed record PricedLine(
        string Code,
        string Description,
        string Category,
        string PricingModel,
        decimal Quantity,
        string? Unit,
        decimal? UnitRate,
        string Markup,
        decimal Buying,
        decimal Selling,
        bool Priced);

    public QuotationPdfDocument(QuotationPdfRequest request)
    {
        _quote = request.Quotation;
        _tenant = request.Tenant;
        _customerName = request.CustomerName;
        _customerAddress = request.CustomerAddress;
        _locationNames = request.LocationNames;
        _logo = PdfLogo.TryDecode(request.LogoBytes);
        _lines = BuildLines(request);
    }

    /// <summary>
    /// Prices every line through the aggregate so percentage lines resolve their valuation
    /// base. A line whose base cannot be derived yet (a draft quotation) still prints, with
    /// its amount left blank rather than failing the whole offer.
    /// </summary>
    private List<PricedLine> BuildLines(QuotationPdfRequest request)
    {
        var result = new List<PricedLine>();

        foreach (var line in _quote.Lines)
        {
            var rate = line.TariffRate;
            var charge = rate?.ChargeCode;

            decimal buying = 0m;
            decimal selling = 0m;
            var priced = true;

            try
            {
                var prices = _quote.PriceLine(line);
                buying = prices.Buying.Amount;
                selling = prices.Selling.Amount;
            }
            catch (InvalidOperationException)
            {
                priced = false;
            }

            result.Add(new PricedLine(
                Code: charge?.Code ?? "—",
                Description: charge?.Name ?? "Unspecified Charge",
                Category: PdfFormatting.Display(line.Category),
                PricingModel: rate is null ? "—" : PdfFormatting.Display(rate.PricingModel),
                Quantity: line.Quantity,
                Unit: line.TariffRate?.PricingModel == PricingModel.Percentage
                    ? $"{line.TariffRate.Percentage}{line.TariffRate?.Unit}"
                    : line.TariffRate?.Unit,
                UnitRate: rate?.BaseAmount,
                Markup: DescribeMarkup(line),
                Buying: buying,
                Selling: selling,
                Priced: priced));
        }

        return result;
    }

    private static string DescribeMarkup(QuotationLine line) => line.MarkupType switch
    {
        MarkupType.Percentage => $"+{line.MarkupValue.ToString("0.##")}%",
        MarkupType.Flat => $"+{line.MarkupValue.ToString("N2")}",
        _ => "—"
    };

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(9).FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.PaddingBottom(10).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => CompanyInfoColumn.Render(c, _tenant, _logo, includeTaxIds: false));

                row.RelativeItem(2).AlignRight().Column(meta =>
                {
                    meta.Item().Text("FORMAL RATE OFFER")
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    meta.Item().PaddingTop(4).Text($"Date: {DateTime.UtcNow:MMMM dd, yyyy}").SemiBold();
                    meta.Item().Text($"Ref: {_quote.ReferenceNumber}").Bold().FontColor(Colors.Blue.Darken3);

                    if (_quote.ValidUntil.HasValue)
                    {
                        meta.Item().Text($"Valid Until: {_quote.ValidUntil.Value:MMMM dd, yyyy}")
                            .FontColor(Colors.Grey.Darken1);
                    }
                });
            });

            col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().PaddingTop(10).Column(recipient =>
            {
                recipient.Item().Text($"TO: {_customerName}").Bold();

                if (!string.IsNullOrWhiteSpace(_customerAddress))
                {
                    recipient.Item().Text(_customerAddress).FontColor(Colors.Grey.Darken2);
                }

                recipient.Item().PaddingTop(4).Text(
                    $"RE: {PdfFormatting.Display(_quote.FreightMode)} " +
                    $"{PdfFormatting.Display(_quote.Mode)} - {PdfFormatting.Display(_quote.ShipmentType)}").Bold();
            });

            col.Item().PaddingTop(10).Text(text =>
            {
                text.Line($"Dear {_customerName},");
                text.Line("Good day.");
                text.Line("Please find attached our formal rate offer for dedicated logistics services for your perusal.");
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                Spec(row, "Quotation Reference", _quote.ReferenceNumber);
                Spec(row, "Freight Mode", PdfFormatting.Display(_quote.FreightMode));
                Spec(row, "Transport Mode", PdfFormatting.Display(_quote.Mode));
                Spec(row, "Shipment Type", PdfFormatting.Display(_quote.ShipmentType));
                Spec(row, "Incoterm", PdfFormatting.Display(_quote.Incoterm));
                Spec(row, "Currency", _quote.Currency);
            });

            col.Item().PaddingTop(14).Text("OPERATIONAL ROUTING")
                .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

            col.Item().PaddingTop(4).Column(c =>
            {
                var stops = (_quote.Lane?.Stops ?? []).OrderBy(s => s.Sequence).ToList();

                if (stops.Count > 0)
                {
                    c.Item().Text($"• Route: {LocationName(stops.First().LocationId)} to {LocationName(stops.Last().LocationId)} ({stops.Count} stop(s))");
                }

                c.Item().Text($"• Distance: {(_quote.Lane?.TotalDistanceKm ?? 0m):N2} km");

                foreach (var stop in stops)
                {
                    c.Item().Text(
                        $"   {stop.Sequence}. {LocationName(stop.LocationId)} " +
                        $"({PdfFormatting.Display(stop.Type)})" +
                        (stop.WindowEnd.HasValue ? $" - by {stop.WindowEnd.Value:dd MMM yyyy HH:mm}" : string.Empty));
                }
            });

            var cargo = _quote.CargoSpec;
            if (cargo is not null)
            {
                col.Item().PaddingTop(14).Text("CARGO PROFILE")
                    .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

                col.Item().PaddingTop(4).Column(c =>
                {
                    c.Item().Text($"• Commodity: {cargo.Commodity}");
                    c.Item().Text($"• Pallet Count: {cargo.PalletCount} pallet(s) | Gross Weight: {cargo.TotalWeightKg:N2} kg");
                    c.Item().Text($"• Declared Value: {PdfFormatting.Money(cargo.DeclaredCargoValue.Amount, cargo.DeclaredCargoValue.Currency)}");

                    if (cargo.RequiresActiveRefrigeration)
                    {
                        c.Item().Text("• Active refrigeration required.");
                    }
                });
            }

            var temperature = _quote.TemperatureSpec;
            if (temperature is not null)
            {
                col.Item().PaddingTop(14).Text("COLD CHAIN PROFILE")
                    .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

                col.Item().PaddingTop(4).Column(c =>
                {
                    c.Item().Text($"• Zone: {PdfFormatting.Display(temperature.Zone)} | Setpoint: {temperature.SetpointCelsius:N1}°C");
                    c.Item().Text($"• Allowable Variance: ±{temperature.AllowableVarianceCelsius:N1}°C | Compliance: {PdfFormatting.Display(temperature.Compliance)}");
                    c.Item().Text($"• Continuous Telematics: {(temperature.RequiresContinuousTelematics ? "Required" : "Not required")}");
                });
            }

            col.Item().PaddingTop(14).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.6f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(PdfStyles.HeaderCell).Text("Item");
                    header.Cell().Element(PdfStyles.HeaderCell).Text("Charge Description");
                    header.Cell().Element(PdfStyles.HeaderCell).Text("Pricing");
                    header.Cell().Element(PdfStyles.HeaderCell).AlignRight().Text("Quantity");
                    header.Cell().Element(PdfStyles.HeaderCell).AlignRight().Text($"Total ({_quote.Currency})");
                    header.Cell().Element(PdfStyles.HeaderCell).AlignRight().Text("Buying");
                });

                var index = 1;
                foreach (var line in _lines)
                {
                    table.Cell().Element(PdfStyles.Cell).Text(index.ToString("00"));
                    table.Cell().Element(PdfStyles.Cell).Column(cell =>
                    {
                        cell.Item().Text($"{line.Code} - {line.Description}").SemiBold();
                        cell.Item().Text(SubLine(line)).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    table.Cell().Element(PdfStyles.Cell).Text(line.PricingModel);
                    table.Cell().Element(PdfStyles.Cell).AlignRight().Text(line.Quantity.ToString("N2") + " (" + line.Unit + ")");
                    table.Cell().Element(PdfStyles.Cell).AlignRight()
                        .Text(line.Priced ? PdfFormatting.Money(line.Selling, _quote.Currency) : "—")
                        .Bold();
                    table.Cell().Element(PdfStyles.Cell).AlignRight()
                        .Text(line.Priced ? PdfFormatting.Money(line.Buying, _quote.Currency) : "—");
                    index++;
                }
            });

            col.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
            {
                row.RelativeItem().Text("TOTAL ESTIMATED SELLING CHARGES:").Bold().FontColor(Colors.Blue.Darken3);
                row.RelativeItem().AlignRight().Text(PdfFormatting.Money(TotalSelling(), _quote.Currency))
                    .Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
            });

            col.Item().ShowEntire().Column(c =>
            {
                // 1. Terms & Special Conditions / Notes
                if (!string.IsNullOrWhiteSpace(_quote.Notes))
                {
                    //c.Item().PaddingTop(14).Text("TERMS & SPECIAL CONDITIONS:").Bold().FontSize(8);

                    c.Item().PaddingTop(3).Column(terms =>
                    {
                        // Split by line to ensure clean paragraph spacing between numbered points
                        var lines = _quote.Notes.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            terms.Item().PaddingBottom(2).Text(line.Trim()).FontSize(8);
                        }
                    });
                }

                // 2. Separate Quotation Validity Block
                if (_quote.ValidUntil.HasValue)
                {
                    c.Item().PaddingTop(10).Text(text =>
                    {
                        text.Span("Offer Validity: ").Bold().FontSize(8);
                        text.Span($"This quotation is valid until {_quote.ValidUntil.Value:MMMM dd, yyyy}.").FontSize(8);
                    });
                }

                c.Item().PaddingTop(28).Row(sig =>
                {
                    sig.RelativeItem().Column(s =>
                    {
                        s.Item().Text("Issued by:").SemiBold();
                        s.Item().PaddingTop(35).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        s.Item().PaddingTop(3).Text("Commercial Pricing & Operations").Bold();
                        s.Item().Text(_tenant.CompanyName ?? _tenant.Name);
                    });

                    sig.ConstantItem(40);

                    sig.RelativeItem().Column(s =>
                    {
                        s.Item().Text("Conforme & Accepted by:").SemiBold();
                        s.Item().PaddingTop(35).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        s.Item().PaddingTop(3).Text("Authorized Representative Signature").Bold();
                        s.Item().Text(_customerName);
                    });
                });
            });
        });
    }

    private static void Spec(RowDescriptor row, string label, string value) =>
        row.RelativeItem().Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1).SemiBold();
            c.Item().Text(value).Bold();
        });

    private static string SubLine(PricedLine line)
    {
        var parts = new List<string> { line.Category, line.PricingModel };

        if (line.UnitRate.HasValue)
        {
            parts.Add($"Rate {line.UnitRate.Value.ToString("N2")}{(string.IsNullOrWhiteSpace(line.Unit) ? string.Empty : $" / {line.Unit}")}");
        }

        if (line.Markup != "—")
        {
            parts.Add($"Markup {line.Markup}");
        }

        return string.Join(" | ", parts);
    }

    private string LocationName(Guid locationId) =>
        _locationNames.TryGetValue(locationId, out var name) ? name : "Unknown Location";

    private decimal TotalSelling() => _lines.Where(l => l.Priced).Sum(l => l.Selling);

    private void ComposeFooter(IContainer container)
    {
        container.Column(footerCol =>
        {
            footerCol.Item().Element(c => PdfFooter.Render(
                c,
                _tenant,
                "Please confirm your acceptance by signing and returning this offer."));

            footerCol.Item().PaddingTop(4).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }
}
