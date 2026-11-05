using Trackwise.Application.Abstractions.Dispatch;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Services;

internal sealed class DispatchEligibilityService(
    ITenantUnitOfWork tenantUow,
    ILogger<DispatchEligibilityService> logger)
    : IDispatchEligibilityService
{
    /// <summary>
    /// Days before medical-cert expiry at which we surface a warning rather than a hard block.
    /// </summary>
    private const int MedicalCertExpiringWarningDays = 7;

    /// <summary>LTO's B2 ceiling; anything heavier is a code C goods vehicle.</summary>
    private const int LightGoodsVehicleMaxGvwKg = 3_500;

    public async Task<EligibilityResult> CheckAsync(
        Guid truckId,
        Guid loadId,
        Guid? driverId = null,
        CancellationToken ct = default)
    {
        var truck = await tenantUow.Repository<Truck>().GetByIdAsync(truckId, ct);
        if (truck is null)
        {
            return EligibilityResult.Block(new EligibilityIssue(
                EligibilityIssueCode.TruckNotFound,
                EligibilitySeverity.Error,
                $"Truck '{truckId}' not found."));
        }

        var load = await tenantUow.Repository<Load>().GetByIdAsync(loadId, ct);
        if (load is null)
        {
            return EligibilityResult.Block(new EligibilityIssue(
                EligibilityIssueCode.LoadNotFound,
                EligibilitySeverity.Error,
                $"Load '{loadId}' not found."));
        }

        var effectiveDriverId = driverId ?? truck.MainDriverId;
        if (!effectiveDriverId.HasValue)
        {
            return EligibilityResult.Block(new EligibilityIssue(
                EligibilityIssueCode.DriverNotAssigned,
                EligibilitySeverity.Error,
                $"Truck '{truck.Number}' has no driver assigned."));
        }

        var driver = await tenantUow.Repository<Employee>().GetByIdAsync(effectiveDriverId.Value, ct);
        if (driver is null)
        {
            return EligibilityResult.Block(new EligibilityIssue(
                EligibilityIssueCode.DriverNotFound,
                EligibilitySeverity.Error,
                $"Driver '{effectiveDriverId}' not found."));
        }

        var issues = new List<EligibilityIssue>();
        var now = DateTime.UtcNow;

        var primaryLicense = SelectPrimaryLicense(driver, load, now);
        if (primaryLicense is null)
        {
            issues.Add(new(
                EligibilityIssueCode.NoActiveLicense,
                EligibilitySeverity.Error,
                $"Driver {driver.GetFullName()} has no active license suitable for this load."));
        }
        else
        {
            CheckLicense(primaryLicense, truck, load, now, issues);
        }

        CheckTruckAdrEquipment(truck, load, now, issues);

        var hasError = issues.Any(i => i.Severity == EligibilitySeverity.Error);
        if (hasError)
        {
            logger.LogInformation(
                "Eligibility check failed: truck {TruckId} / driver {DriverId} / load {LoadId} â€” {Issues}",
                truckId, effectiveDriverId, loadId,
                string.Join("; ", issues.Select(i => i.Code)));
        }

        return new EligibilityResult(!hasError, issues);
    }

    /// <summary>
    /// Picks the most relevant license for the load: prefer one matching the load's
    /// jurisdiction (US load â†’ US-issued license, PH load â†’ PH-issued), then most recent expiry.
    /// </summary>
    private static DriverLicense? SelectPrimaryLicense(Employee driver, Load load, DateTime now)
    {
        var preferUs = load.OriginAddress.Country is "US" or "USA"
                       || load.DestinationAddress.Country is "US" or "USA";
        var preferPh = load.OriginAddress.Country is "PH" or "PHL"
                       || load.DestinationAddress.Country is "PH" or "PHL";

        return driver.Licenses
            .Where(l => l.Status == DriverLicenseStatus.Active && l.ExpiresAt > now)
            .OrderByDescending(l => (preferUs && l.IssuingCountry == "US")
                                    || (preferPh && l.IssuingCountry == "PH"))
            .ThenByDescending(l => l.ExpiresAt)
            .FirstOrDefault()
            ?? driver.Licenses
                .Where(l => l.Status == DriverLicenseStatus.Active)
                .OrderByDescending(l => l.ExpiresAt)
                .FirstOrDefault();
    }

    private static void CheckLicense(
        DriverLicense license,
        Truck truck,
        Load load,
        DateTime now,
        List<EligibilityIssue> issues)
    {
        if (license.ExpiresAt <= now)
        {
            issues.Add(new(
                EligibilityIssueCode.LicenseExpired,
                EligibilitySeverity.Error,
                $"License {license.LicenseNumber} expired on {license.ExpiresAt:yyyy-MM-dd}."));
            return;
        }

        if (license.MedicalCertExpiresAt.HasValue)
        {
            var medCert = license.MedicalCertExpiresAt.Value;
            if (medCert <= now)
            {
                issues.Add(new(
                    EligibilityIssueCode.MedicalCertExpired,
                    EligibilitySeverity.Error,
                    $"DOT medical certificate expired on {medCert:yyyy-MM-dd}."));
            }
            else if ((medCert - now).TotalDays <= MedicalCertExpiringWarningDays)
            {
                issues.Add(new(
                    EligibilityIssueCode.MedicalCertExpiringSoon,
                    EligibilitySeverity.Warning,
                    $"DOT medical certificate expires on {medCert:yyyy-MM-dd}."));
            }
        }

        if (license.LicenseClass.IsPhilippine())
        {
            CheckPhilippineLicense(license, truck, issues);

            // Hazmat endorsements are US CDL / EU ADR concepts; Philippine hazmat licensing isn't
            // modelled, so the ADR rules below would block every PH driver on a hazmat load.
            return;
        }

        if (!load.IsHazmat)
        {
            return;
        }

        var isUSLicense = license.IssuingCountry == "US";
        if (isUSLicense)
        {
            if (!license.HasEndorsement(LicenseEndorsement.Hazmat))
            {
                issues.Add(new(
                    EligibilityIssueCode.MissingHazmatEndorsement,
                    EligibilitySeverity.Error,
                    "US driver requires Hazmat (H) endorsement for this load."));
            }
            return;
        }

        // EU / non-US license â€” require ADR.
        if (!license.HasEndorsement(LicenseEndorsement.Adr))
        {
            issues.Add(new(
                EligibilityIssueCode.MissingAdrEndorsement,
                EligibilitySeverity.Error,
                "Driver requires ADR endorsement for this load."));
        }

        if (load.HazmatClass == HazmatClass.Class1
            && !license.HasEndorsement(LicenseEndorsement.AdrClass1))
        {
            issues.Add(new(
                EligibilityIssueCode.MissingAdrClass1Endorsement,
                EligibilitySeverity.Error,
                "ADR Class 1 (Explosives) endorsement required for this load."));
        }

        if (load.HazmatClass == HazmatClass.Class7
            && !license.HasEndorsement(LicenseEndorsement.AdrClass7))
        {
            issues.Add(new(
                EligibilityIssueCode.MissingAdrClass7Endorsement,
                EligibilitySeverity.Error,
                "ADR Class 7 (Radioactive) endorsement required for this load."));
        }
    }

    /// <summary>
    /// LTO rules: driving for hire needs a Professional license, and the DL codes must cover the
    /// truck - CE for an articulated unit, otherwise B2 up to 3,500 kg GVW and C above it. Without
    /// a GVW on file the truck could be either, so B2 is let through with a warning to check.
    /// </summary>
    private static void CheckPhilippineLicense(
        DriverLicense license,
        Truck truck,
        List<EligibilityIssue> issues)
    {
        if (license.LicenseClass == LicenseClass.PHNonProfessional)
        {
            issues.Add(new(
                EligibilityIssueCode.LicenseNotProfessional,
                EligibilitySeverity.Error,
                $"License {license.LicenseNumber} is Non-Professional; driving for hire needs a Professional license."));
        }

        var codes = license.DlCodes == PhilippineDlCode.None
            ? "none"
            : string.Join(", ", license.DlCodes.ToArray());

        if (truck.Type is TruckType.ContainerTruck or TruckType.LowLoader)
        {
            if (!license.HasDlCode(PhilippineDlCode.CE))
            {
                issues.Add(new(
                    EligibilityIssueCode.LicenseClassInsufficient,
                    EligibilitySeverity.Error,
                    $"Truck {truck.Number} pulls a trailer and needs DL code CE; license {license.LicenseNumber} has {codes}."));
            }

            return;
        }

        if (license.HasDlCode(PhilippineDlCode.C))
        {
            return;
        }

        if (truck.GrossVehicleWeightKg is > LightGoodsVehicleMaxGvwKg)
        {
            issues.Add(new(
                EligibilityIssueCode.LicenseClassInsufficient,
                EligibilitySeverity.Error,
                $"Truck {truck.Number} is {truck.GrossVehicleWeightKg:N0} kg GVW and needs DL code C; license {license.LicenseNumber} has {codes}."));
            return;
        }

        if (!license.HasDlCode(PhilippineDlCode.B2))
        {
            issues.Add(new(
                EligibilityIssueCode.LicenseClassInsufficient,
                EligibilitySeverity.Error,
                $"Truck {truck.Number} needs DL code B2 or C; license {license.LicenseNumber} has {codes}."));
            return;
        }

        if (truck.GrossVehicleWeightKg is null)
        {
            issues.Add(new(
                EligibilityIssueCode.GrossVehicleWeightUnknown,
                EligibilitySeverity.Warning,
                $"Truck {truck.Number} has no gross vehicle weight on file. If it's over 3,500 kg the driver needs DL code C, and license {license.LicenseNumber} only has {codes}."));
        }
    }

    private static void CheckTruckAdrEquipment(
        Truck truck,
        Load load,
        DateTime now,
        List<EligibilityIssue> issues)
    {
        if (!load.IsHazmat)
        {
            return;
        }

        var isUSRoute = (load.OriginAddress.Country is "US" or "USA")
                        || (load.DestinationAddress.Country is "US" or "USA");

        if (isUSRoute)
        {
            if (!truck.IsHazmatPlacarded)
            {
                issues.Add(new(
                    EligibilityIssueCode.HazmatPlacardingRequired,
                    EligibilitySeverity.Error,
                    $"Truck {truck.Number} is not Hazmat-placarded for US Hazmat transport."));
            }
            return;
        }

        // EU / ADR side
        if (!truck.AdrEquipment.IsAdrCertified)
        {
            issues.Add(new(
                EligibilityIssueCode.TruckNotAdrCertified,
                EligibilitySeverity.Error,
                $"Truck {truck.Number} is not ADR-certified."));
            return;
        }

        if (truck.AdrEquipment.IsCertExpired(now))
        {
            issues.Add(new(
                EligibilityIssueCode.AdrCertExpired,
                EligibilitySeverity.Error,
                $"ADR certificate for truck {truck.Number} expired on " +
                $"{truck.AdrEquipment.AdrCertExpiresAt:yyyy-MM-dd}."));
            return;
        }

        if (load.HazmatClass.HasValue && !truck.AdrEquipment.AllowsClass(load.HazmatClass.Value))
        {
            issues.Add(new(
                EligibilityIssueCode.TruckClassNotAllowed,
                EligibilitySeverity.Error,
                $"Truck {truck.Number} is not certified for {load.HazmatClass.Value.GetDescription()}."));
        }
    }
}
