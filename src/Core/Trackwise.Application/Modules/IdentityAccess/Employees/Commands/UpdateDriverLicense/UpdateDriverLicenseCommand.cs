using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

public class UpdateDriverLicenseCommand : ICommand
{
    public Guid LicenseId { get; set; }
    public LicenseClass? LicenseClass { get; set; }
    public LicenseEndorsement[]? Endorsements { get; set; }
    public PhilippineDlCode[]? DlCodes { get; set; }
    public string? IssuingRegion { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? MedicalCertExpiresAt { get; set; }
    public DriverLicenseStatus? Status { get; set; }
    public Guid? DocumentId { get; set; }
}
