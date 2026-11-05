using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.IdentityAccess.Tenants;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.GenerateQuotationPdf;

internal sealed class GenerateQuotationPdfHandler(
    ITenantUnitOfWork tenantUow,
    IQuotationPdfGenerator pdfGenerator,
    IBlobStorageService blobStorage)
    : IAppRequestHandler<GenerateQuotationPdfCommand, Result<FileResponse>>
{
    public async Task<Result<FileResponse>> Handle(GenerateQuotationPdfCommand req, CancellationToken ct)
    {
        // The tariff rate and its charge code carry the description, unit and rate the PDF prints,
        // and the line pricing throws without them.
        var quotation = await tenantUow.Repository<Quotation>().Query()
            .Include(q => q.Lines)
                .ThenInclude(l => l.TariffRate)
                    .ThenInclude(tr => tr!.ChargeCode)
            .Include(q => q.Lines)
                .ThenInclude(l => l.TariffRate)
                    .ThenInclude(tr => tr!.Tariff)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == req.QuotationId, ct);

        if (quotation is null)
            return Result<FileResponse>.Fail($"Quotation with id '{req.QuotationId}' not found", ErrorCodes.NotFound);

        var customer = await tenantUow.Repository<Customer>().GetByIdAsync(quotation.CustomerId, ct);

        var stopLocationIds = (quotation.Lane?.Stops ?? [])
            .Select(s => s.LocationId)
            .Distinct()
            .ToList();

        var locationNames = stopLocationIds.Count > 0
            ? await tenantUow.Repository<Location>().Query()
                .Where(l => stopLocationIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => string.IsNullOrWhiteSpace(l.Name) ? l.Code : l.Name, ct)
            : new Dictionary<Guid, string>();

        var tenant = tenantUow.GetCurrentTenant();
        var logo = await TenantLogoReader.ReadAsync(blobStorage, tenant, ct);

        byte[] pdfBytes;
        try
        {
            pdfBytes = pdfGenerator.Generate(new QuotationPdfRequest(
                Quotation: quotation,
                Tenant: tenant,
                CustomerName: customer?.Name ?? "Customer",
                CustomerAddress: FormatAddress(customer?.Address),
                LocationNames: locationNames,
                LogoBytes: logo));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<FileResponse>.Fail($"The quotation PDF could not be generated: {ex.Message}");
        }

        return Result<FileResponse>.Ok(new FileResponse(
            pdfBytes,
            "application/pdf",
            $"Quotation-{quotation.ReferenceNumber}.pdf"));
    }

    private static string? FormatAddress(Address? address)
    {
        if (address is null) return null;

        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(address.StreetAddress)) lines.Add(address.StreetAddress);
        if (!string.IsNullOrWhiteSpace(address.Barangay)) lines.Add(address.Barangay);

        var city = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.CityOrMunicipality)) city.Add(address.CityOrMunicipality);
        if (!string.IsNullOrWhiteSpace(address.Province)) city.Add(address.Province);
        if (!string.IsNullOrWhiteSpace(address.Zipcode)) city.Add(address.Zipcode);
        if (city.Count > 0) lines.Add(string.Join(" ", city));

        if (!string.IsNullOrWhiteSpace(address.Country)) lines.Add(address.Country);

        return lines.Count == 0 ? null : string.Join(", ", lines);
    }
}
