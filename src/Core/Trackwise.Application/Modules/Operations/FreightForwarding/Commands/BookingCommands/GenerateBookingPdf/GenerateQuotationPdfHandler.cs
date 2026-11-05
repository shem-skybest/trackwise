using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.IdentityAccess.Tenants;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.GenerateQuotationPdf;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.GenerateBookingPdf;

internal sealed class GenerateBookingPdfHandler(
    ITenantUnitOfWork tenantUow,
    IBookingPdfGenerator pdfGenerator,
    IBlobStorageService blobStorage)
    : IAppRequestHandler<GenerateBookingPdfCommand, Result<FileResponse>>
{
    public async Task<Result<FileResponse>> Handle(GenerateBookingPdfCommand req, CancellationToken ct)
    {
        // The booking carries its own locked charges and documents, and its lane is a JSON column on the
        // row, so unlike the quotation there is no TariffRate/ChargeCode graph to load: the PDF prints
        // the stored snapshots and never reprices anything.
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .Include(b => b.Documents)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == req.BookingId, ct);

        if (booking is null)
            return Result<FileResponse>.Fail($"Booking with id '{req.BookingId}' not found", ErrorCodes.NotFound);

        var customer = await tenantUow.Repository<Customer>().GetByIdAsync(booking.CustomerId, ct);

        var stopLocationIds = (booking.Lane?.Stops ?? [])
            .Select(s => s.LocationId)
            .Distinct()
            .ToList();

        var locationNames = stopLocationIds.Count > 0
            ? await tenantUow.Repository<Location>().Query().AsNoTracking()
                .Where(l => stopLocationIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => string.IsNullOrWhiteSpace(l.Name) ? l.Code : l.Name, ct)
            : new Dictionary<Guid, string>();

        var tenant = tenantUow.GetCurrentTenant();
        var logo = await TenantLogoReader.ReadAsync(blobStorage, tenant, ct);

        byte[] pdfBytes;
        try
        {
            pdfBytes = pdfGenerator.Generate(new BookingPdfRequest(
                Booking: booking,
                Tenant: tenant,
                CustomerName: customer?.Name ?? "Customer",
                CustomerAddress: AddressFormatter.ToSingleLine(customer?.Address),
                LocationNames: locationNames,
                LogoBytes: logo,
                IncludeInternalCosts: req.IncludeInternalCosts));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<FileResponse>.Fail($"The booking PDF could not be generated: {ex.Message}");
        }

        // An internal copy is named differently so it can't be mistaken for the customer's document.
        var suffix = req.IncludeInternalCosts ? "-INTERNAL" : string.Empty;

        return Result<FileResponse>.Ok(new FileResponse(
            pdfBytes,
            "application/pdf",
            $"Booking-{booking.BookingNumber}{suffix}.pdf"));
    }
}
