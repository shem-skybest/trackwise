using Trackwise.Application.Abstractions;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

internal sealed class UpdateDriverLicenseHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateDriverLicenseCommand, Result>
{
    public async Task<Result> Handle(UpdateDriverLicenseCommand req, CancellationToken ct)
    {
        var license = await tenantUow.Repository<DriverLicense>().GetByIdAsync(req.LicenseId, ct);
        if (license is null)
        {
            return Result.Fail($"Driver license not found with ID '{req.LicenseId}'");
        }

        if (req.DocumentId.HasValue)
        {
            var doc = await tenantUow.Repository<EmployeeDocument>().GetByIdAsync(req.DocumentId.Value, ct);
            if (doc is null)
            {
                return Result.Fail($"Document '{req.DocumentId}' not found");
            }
            if (doc.EmployeeId != license.EmployeeId)
            {
                return Result.Fail(
                    $"Document '{req.DocumentId}' belongs to a different employee and cannot be attached to this license");
            }
        }

        license.LicenseClass = PropertyUpdater.UpdateIfChanged(req.LicenseClass, license.LicenseClass);
        if (req.Endorsements is not null)
        {
            license.Endorsements = req.Endorsements.Combine();
        }

        if (req.DlCodes is not null)
        {
            license.DlCodes = req.DlCodes.Combine();
        }

        // The issuing country is fixed after creation, so a class change has to stay within it.
        if (!license.LicenseClass.IsValidForCountry(license.IssuingCountry))
        {
            return Result.Fail("License class is not valid for the issuing country.");
        }

        if (!license.LicenseClass.IsPhilippine())
        {
            license.DlCodes = PhilippineDlCode.None;
        }
        else if (license.DlCodes == PhilippineDlCode.None)
        {
            return Result.Fail("A Philippine license needs at least one DL code (e.g. B2, C, CE).");
        }
        license.IssuingRegion = PropertyUpdater.UpdateIfChanged(req.IssuingRegion, license.IssuingRegion);
        license.IssuedDate = PropertyUpdater.UpdateIfChanged(req.IssuedDate, license.IssuedDate);
        license.ExpiresAt = PropertyUpdater.UpdateIfChanged(req.ExpiresAt, license.ExpiresAt);
        license.Status = PropertyUpdater.UpdateIfChanged(req.Status, license.Status);

        // Nullable-struct targets - PropertyUpdater has no overload for these.
        if (req.MedicalCertExpiresAt.HasValue)
        {
            license.MedicalCertExpiresAt = req.MedicalCertExpiresAt;
        }
        if (req.DocumentId.HasValue)
        {
            license.DocumentId = req.DocumentId;
        }

        if (license.ExpiresAt <= license.IssuedDate)
        {
            return Result.Fail("Expiry must be after issued date.");
        }

        tenantUow.Repository<DriverLicense>().Update(license);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
