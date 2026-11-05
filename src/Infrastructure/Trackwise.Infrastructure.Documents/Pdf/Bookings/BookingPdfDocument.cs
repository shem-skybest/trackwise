using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Infrastructure.Documents.Pdf.Shared;

namespace Trackwise.Infrastructure.Documents.Pdf.Bookings;

/// <summary>
/// The booking confirmation sent to the customer: shipment details, route, cargo, the locked
/// charges, payment terms and the documents still needed. Everything it prints comes from the
/// booking aggregate plus the names the application layer resolved for it.
/// Buying cost and margin are internal and only appear on an explicit internal copy.
/// </summary>
public sealed class BookingPdfDocument : IDocument
{
    private static readonly TimeSpan DefaultUtcOffset = TimeSpan.FromHours(8);

    private readonly Booking _booking;
    private readonly Tenant _tenant;
    private readonly Image? _logo;
    private readonly string _customerName;
    private readonly string? _customerAddress;
    private readonly IReadOnlyDictionary<Guid, string> _locationNames;
    private readonly bool _includeCosts;
    private readonly TimeSpan _offset;
    private readonly IReadOnlyList<ChargeRow> _charges;
    private readonly BookingTotals _totals;

    private sealed record ChargeRow(
        string Description,
        string SubLine,
        decimal Quantity,
        decimal Selling,
        decimal Buying);

    public BookingPdfDocument(BookingPdfRequest request)
    {
        _booking = request.Booking;
        _tenant = request.Tenant;
        _customerName = request.CustomerName;
        _customerAddress = request.CustomerAddress;
        _locationNames = request.LocationNames;
        _includeCosts = request.IncludeInternalCosts;
        _offset = request.UtcOffset ?? DefaultUtcOffset;
        _logo = PdfLogo.TryDecode(request.LogoBytes);
        _charges = BuildCharges();
        _totals = _booking.GetTotals();
    }

    // ── Data shaping ─────────────────────────────────────────────────────────

    /// <summary>
    /// Charges come straight from the booking's locked snapshots: nothing is repriced here, so the
    /// PDF always agrees with what Billing will invoice. Quoted lines first, then added ones.
    /// </summary>
    private List<ChargeRow> BuildCharges() =>
        _booking.Charges
            .OrderBy(c => c.Source)
            .ThenBy(c => c.CreatedAt)
            .Select(c => new ChargeRow(
                Description: c.Description,
                SubLine: DescribeCharge(c),
                Quantity: c.Quantity,
                Selling: c.SellingAmount,
                Buying: c.BuyingAmount))
            .ToList();

    private static string DescribeCharge(BookingChargeLine charge)
    {
        var parts = new List<string> { PdfFormatting.Display(charge.Category) };

        if (charge.Source == BookingChargeSource.Additional)
        {
            parts.Add("Additional charge");
            if (!string.IsNullOrWhiteSpace(charge.Reason))
            {
                parts.Add(charge.Reason!);
            }
        }

        return string.Join(" | ", parts);
    }

    private string Title => _booking.Status switch
    {
        BookingStatus.Draft => "BOOKING (DRAFT)",
        BookingStatus.Cancelled => "BOOKING CANCELLED",
        _ => "BOOKING CONFIRMED"
    };

    private DateTimeOffset Local(DateTimeOffset value) => value.ToOffset(_offset);

    private string LocationName(Guid locationId) =>
        _locationNames.TryGetValue(locationId, out var name) ? name : "Unknown Location";

    private string Window(RouteStop stop) => (stop.WindowStart, stop.WindowEnd) switch
    {
        ({ } start, { } end) => $"{Local(start):dd MMM yyyy HH:mm} to {Local(end):dd MMM yyyy HH:mm}",
        ({ } start, null) => $"from {Local(start):dd MMM yyyy HH:mm}",
        (null, { } end) => $"by {Local(end):dd MMM yyyy HH:mm}",
        _ => string.Empty
    };

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{_booking.BookingNumber} - {_customerName}",
        Author = _tenant.CompanyName ?? _tenant.Name
    };

    // ── Layout ───────────────────────────────────────────────────────────────

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
        var issuedOn = Local(_booking.ConfirmedAt ?? DateTimeOffset.UtcNow);

        container.PaddingBottom(10).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => CompanyInfoColumn.Render(c, _tenant, _logo, includeTaxIds: false));

                row.RelativeItem(2).AlignRight().Column(meta =>
                {
                    meta.Item().Text(Title)
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    meta.Item().PaddingTop(4).Text($"Date: {issuedOn:MMMM dd, yyyy}").SemiBold();
                    meta.Item().Text($"Booking No: {_booking.BookingNumber}").Bold().FontColor(Colors.Blue.Darken3);
                    meta.Item().Text($"Quotation: {_booking.QuotationReference}").FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(_booking.CustomerReference))
                    {
                        meta.Item().Text($"Your Ref: {_booking.CustomerReference}").FontColor(Colors.Grey.Darken1);
                    }
                });
            });

            col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            if (_includeCosts)
            {
                col.Item().PaddingTop(8).Background(Colors.Orange.Lighten4).Padding(5)
                    .Text("INTERNAL COPY - includes buying costs and margin. Do not send to the customer.")
                    .Bold().FontSize(8).FontColor(Colors.Orange.Darken4);
            }

            if (_booking.Status == BookingStatus.Cancelled)
            {
                col.Item().PaddingTop(8).Background(Colors.Red.Lighten4).Padding(6).Column(c =>
                {
                    c.Item().Text("THIS BOOKING HAS BEEN CANCELLED").Bold().FontColor(Colors.Red.Darken3);

                    if (!string.IsNullOrWhiteSpace(_booking.CancellationReason))
                    {
                        c.Item().Text($"Reason: {_booking.CancellationReason}").FontSize(8);
                    }
                });
            }
            else if (_booking.Status == BookingStatus.Draft)
            {
                col.Item().PaddingTop(8).Background(Colors.Grey.Lighten3).Padding(6)
                    .Text("DRAFT - this booking is not yet confirmed and may still change.")
                    .Bold().FontSize(8).FontColor(Colors.Grey.Darken3);
            }

            col.Item().PaddingTop(10).Column(recipient =>
            {
                recipient.Item().Text($"TO: {_customerName}").Bold();

                if (!string.IsNullOrWhiteSpace(_customerAddress))
                {
                    recipient.Item().Text(_customerAddress).FontColor(Colors.Grey.Darken2);
                }

                recipient.Item().PaddingTop(4).Text(
                    $"RE: {PdfFormatting.Display(_booking.FreightMode)} " +
                    $"{PdfFormatting.Display(_booking.Mode)} - {PdfFormatting.Display(_booking.ShipmentType)}").Bold();
            });

            col.Item().PaddingTop(10).Text(text =>
            {
                text.Line($"Dear {_customerName},");
                text.Line("Good day.");
                text.Line(_booking.Status == BookingStatus.Draft
                    ? "Please find the details of your booking below. It will be confirmed once the open items are completed."
                    : "Thank you for your business. Please find the details of your booking below.");
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(8).Column(specs =>
            {
                specs.Item().Row(row =>
                {
                    Spec(row, "Booking No.", _booking.BookingNumber);
                    Spec(row, "Quotation Reference", _booking.QuotationReference);
                    Spec(row, "Customer Reference", string.IsNullOrWhiteSpace(_booking.CustomerReference) ? "—" : _booking.CustomerReference!);
                    Spec(row, "Currency", _booking.Currency);
                });

                specs.Item().PaddingTop(6).Row(row =>
                {
                    Spec(row, "Freight Mode", PdfFormatting.Display(_booking.FreightMode));
                    Spec(row, "Transport Mode", PdfFormatting.Display(_booking.Mode));
                    Spec(row, "Shipment Type", PdfFormatting.Display(_booking.ShipmentType));
                    Spec(row, "Incoterm", PdfFormatting.Display(_booking.Incoterm));
                });
            });

            ComposeRouting(col);
            ComposeCargo(col);
            ComposeColdChain(col);
            ComposeHandling(col);
            ComposeCharges(col);
            ComposePayment(col);
            ComposeDocuments(col);
            ComposeClosing(col);
        });
    }

    private void ComposeRouting(ColumnDescriptor col)
    {
        var stops = (_booking.Lane?.Stops ?? []).OrderBy(s => s.Sequence).ToList();

        col.Item().PaddingTop(14).Text("ROUTING & SCHEDULE")
            .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

        col.Item().PaddingTop(4).Column(c =>
        {
            if (stops.Count > 0)
            {
                c.Item().Text($"• Route: {LocationName(stops.First().LocationId)} to {LocationName(stops.Last().LocationId)} ({stops.Count} stop(s))");
            }

            c.Item().Text($"• Distance: {(_booking.Lane?.TotalDistanceKm ?? 0m):N2} km");

            if (_booking.RequestedPickupStart is { } pickup)
            {
                c.Item().Text($"• Requested pickup: {Local(pickup):dd MMM yyyy HH:mm}");
            }

            if (_booking.RequestedDeliveryEnd is { } delivery)
            {
                c.Item().Text($"• Requested delivery by: {Local(delivery):dd MMM yyyy HH:mm}");
            }

            foreach (var stop in stops)
            {
                var window = Window(stop);
                c.Item().Text(
                    $"   {stop.Sequence}. {LocationName(stop.LocationId)} " +
                    $"({PdfFormatting.Display(stop.Type)})" +
                    (window.Length > 0 ? $" - {window}" : string.Empty));

                if (!string.IsNullOrWhiteSpace(stop.Instructions))
                {
                    c.Item().PaddingLeft(14).Text(stop.Instructions!).FontSize(8).FontColor(Colors.Grey.Darken1);
                }
            }
        });
    }

    private void ComposeCargo(ColumnDescriptor col)
    {
        var cargo = _booking.CargoSpec;
        if (cargo is null)
        {
            return;
        }

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

    private void ComposeColdChain(ColumnDescriptor col)
    {
        var temperature = _booking.TemperatureSpec;
        if (temperature is null)
        {
            return;
        }

        col.Item().PaddingTop(14).Text("COLD CHAIN PROFILE")
            .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

        col.Item().PaddingTop(4).Column(c =>
        {
            c.Item().Text($"• Zone: {PdfFormatting.Display(temperature.Zone)} | Setpoint: {temperature.SetpointCelsius:N1}°C");
            c.Item().Text($"• Allowable Variance: ±{temperature.AllowableVarianceCelsius:N1}°C | Compliance: {PdfFormatting.Display(temperature.Compliance)}");
            c.Item().Text($"• Continuous Telematics: {(temperature.RequiresContinuousTelematics ? "Required" : "Not required")}");
        });
    }

    private void ComposeHandling(ColumnDescriptor col)
    {
        var flags = new List<string>();

        if (_booking.IsHazardous)
        {
            flags.Add("Dangerous goods - handled under Hazmat compliance.");
        }

        if (_booking.RequiresCustomsBrokerage)
        {
            flags.Add("Customs brokerage is included in this shipment.");
        }

        if (flags.Count == 0)
        {
            return;
        }

        col.Item().PaddingTop(14).Text("SPECIAL HANDLING")
            .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

        col.Item().PaddingTop(4).Column(c =>
        {
            foreach (var flag in flags)
            {
                c.Item().Text($"• {flag}");
            }
        });
    }

    private void ComposeCharges(ColumnDescriptor col)
    {
        col.Item().PaddingTop(14).Text("CHARGES")
            .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

        col.Item().PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(5);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.8f);

                if (_includeCosts)
                {
                    columns.RelativeColumn(1.8f);
                }
            });

            table.Header(header =>
            {
                header.Cell().Element(PdfStyles.HeaderCell).Text("Item");
                header.Cell().Element(PdfStyles.HeaderCell).Text("Charge Description");
                header.Cell().Element(PdfStyles.HeaderCell).AlignRight().Text("Quantity");
                header.Cell().Element(PdfStyles.HeaderCell).AlignRight().Text($"Total ({_booking.Currency})");

                if (_includeCosts)
                {
                    header.Cell().Element(PdfStyles.HeaderCell).AlignRight().Text("Buying");
                }
            });

            if (_charges.Count == 0)
            {
                // A table needs at least one row; say so instead of printing a bare header.
                table.Cell().ColumnSpan((uint)(_includeCosts ? 5 : 4)).Element(PdfStyles.Cell)
                    .Text("No charges recorded.").FontColor(Colors.Grey.Darken1);
                return;
            }

            var index = 1;
            foreach (var charge in _charges)
            {
                table.Cell().Element(PdfStyles.Cell).Text(index.ToString("00"));
                table.Cell().Element(PdfStyles.Cell).Column(cell =>
                {
                    cell.Item().Text(charge.Description).SemiBold();
                    cell.Item().Text(charge.SubLine).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                table.Cell().Element(PdfStyles.Cell).AlignRight().Text(charge.Quantity.ToString("N2"));
                table.Cell().Element(PdfStyles.Cell).AlignRight()
                    .Text(PdfFormatting.Money(charge.Selling, _booking.Currency)).Bold();

                if (_includeCosts)
                {
                    table.Cell().Element(PdfStyles.Cell).AlignRight()
                        .Text(PdfFormatting.Money(charge.Buying, _booking.Currency));
                }

                index++;
            }
        });

        col.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
        {
            row.RelativeItem().Text("TOTAL CHARGES:").Bold().FontColor(Colors.Blue.Darken3);
            row.RelativeItem().AlignRight().Text(PdfFormatting.Money(_totals.Selling, _booking.Currency))
                .Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
        });

        if (_includeCosts)
        {
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("Internal: buying total / margin").FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(
                        $"{PdfFormatting.Money(_totals.Buying, _booking.Currency)} / " +
                        $"{PdfFormatting.Money(_totals.Margin, _booking.Currency)} ({_totals.MarginPercent:0.##}%)")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        }
    }

    private void ComposePayment(ColumnDescriptor col)
    {
        col.Item().PaddingTop(14).Text("PAYMENT TERMS")
            .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

        col.Item().PaddingTop(4).Column(c =>
        {
            c.Item().Text(_booking.PaymentTermsDays == 0
                ? "• Payment is due on receipt of invoice."
                : $"• Payment is due {_booking.PaymentTermsDays} day(s) from the invoice date.");

            if (_booking.DepositRequiredPercent is { } percent && percent > 0m)
            {
                var deposit = Math.Round(_totals.Selling * percent / 100m, 2);
                c.Item().Text(
                    $"• A deposit of {percent:0.##}% ({PdfFormatting.Money(deposit, _booking.Currency)}) " +
                    "is required upon confirmation of this booking.");
            }
        });
    }

    private void ComposeDocuments(ColumnDescriptor col)
    {
        var documents = _booking.Documents.OrderBy(d => d.Type).ToList();
        if (documents.Count == 0)
        {
            return;
        }

        var outstanding = documents.Count(d => !d.IsSatisfied);

        col.Item().PaddingTop(14).Text("DOCUMENTS & REQUIREMENTS")
            .Bold().FontSize(9).FontColor(Colors.Blue.Darken3);

        if (_booking.Status == BookingStatus.Draft && outstanding > 0)
        {
            col.Item().PaddingTop(4)
                .Text($"{outstanding} item(s) below must be completed before this booking can be confirmed.")
                .FontSize(8).FontColor(Colors.Grey.Darken2);
        }

        col.Item().PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2.5f);
            });

            table.Header(header =>
            {
                header.Cell().Element(PdfStyles.HeaderCell).Text("Document");
                header.Cell().Element(PdfStyles.HeaderCell).Text("Status");
                header.Cell().Element(PdfStyles.HeaderCell).Text("Reference / Expiry");
            });

            foreach (var doc in documents)
            {
                table.Cell().Element(PdfStyles.Cell).Text(DocumentLabel(doc.Type)).SemiBold();
                table.Cell().Element(PdfStyles.Cell).Text(DocumentStatus(doc));
                table.Cell().Element(PdfStyles.Cell).Text(DocumentReference(doc));
            }
        });
    }

    private string DocumentStatus(BookingDocument doc) =>
        doc.Status is BookingDocumentStatus.Verified or BookingDocumentStatus.Waived && !doc.IsSatisfied
            ? "Expired"
            : PdfFormatting.Display(doc.Status);

    private string DocumentReference(BookingDocument doc)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(doc.Reference))
        {
            parts.Add(doc.Reference!);
        }

        if (doc.ExpiresAt is { } expiresAt)
        {
            parts.Add($"expires {Local(expiresAt):dd MMM yyyy}");
        }

        return parts.Count == 0 ? "—" : string.Join(" | ", parts);
    }

    private static string DocumentLabel(BookingDocumentType type) => type switch
    {
        BookingDocumentType.Kyc => "KYC (Know Your Customer)",
        _ => PdfFormatting.Display(type)
    };

    private void ComposeClosing(ColumnDescriptor col)
    {
        col.Item().ShowEntire().Column(c =>
        {
            if (!string.IsNullOrWhiteSpace(_booking.Notes))
            {
                c.Item().PaddingTop(14).Text("NOTES:").Bold().FontSize(8);

                c.Item().PaddingTop(3).Column(notes =>
                {
                    // One paragraph per line so numbered points keep their spacing
                    var lines = _booking.Notes.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        notes.Item().PaddingBottom(2).Text(line.Trim()).FontSize(8);
                    }
                });
            }

            c.Item().PaddingTop(10).Text(
                    $"This booking is governed by the terms of accepted quotation {_booking.QuotationReference}.")
                .FontSize(8).FontColor(Colors.Grey.Darken1);

            c.Item().PaddingTop(28).Column(s =>
            {
                s.Item().Text("Issued by:").SemiBold();
                s.Item().Width(220).PaddingTop(35).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                s.Item().PaddingTop(3).Text("Customer Service & Operations").Bold();
                s.Item().Text(_tenant.CompanyName ?? _tenant.Name);
            });
        });
    }

    private static void Spec(RowDescriptor row, string label, string value) =>
        row.RelativeItem().Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1).SemiBold();
            c.Item().Text(value).Bold();
        });

    private void ComposeFooter(IContainer container)
    {
        container.Column(footerCol =>
        {
            footerCol.Item().Element(c => PdfFooter.Render(
                c,
                _tenant,
                _booking.Status == BookingStatus.Draft
                    ? "This draft is for review only and is not a confirmed booking."
                    : "Please quote the booking number in all correspondence."));

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
