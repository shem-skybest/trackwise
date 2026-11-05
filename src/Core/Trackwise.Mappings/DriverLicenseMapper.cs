using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class DriverLicenseMapper
{
    public static DriverLicenseDto ToDto(this DriverLicense entity)
    {
        return new DriverLicenseDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            LicenseNumber = entity.LicenseNumber,
            LicenseClass = entity.LicenseClass,
            Endorsements = entity.Endorsements.ToArray(),
            DlCodes = entity.DlCodes.ToArray(),
            IssuingCountry = entity.IssuingCountry,
            IssuingRegion = entity.IssuingRegion,
            IssuedDate = entity.IssuedDate,
            ExpiresAt = entity.ExpiresAt,
            MedicalCertExpiresAt = entity.MedicalCertExpiresAt,
            Status = entity.Status,
            DocumentId = entity.DocumentId,
            DaysUntilExpiry = (int)(entity.ExpiresAt - DateTime.UtcNow).TotalDays
        };
    }
}
