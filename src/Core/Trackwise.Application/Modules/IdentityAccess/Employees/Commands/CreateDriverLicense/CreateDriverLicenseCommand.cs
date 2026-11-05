using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

public class CreateDriverLicenseCommand : ICommand<Result<Guid>>
{
    public Guid EmployeeId { get; set; }
    public string LicenseNumber { get; set; } = null!;
    public LicenseClass LicenseClass { get; set; }
    public LicenseEndorsement[] Endorsements { get; set; } = [];

    /// <summary>LTO DL codes on a Philippine license; must be empty for any other country.</summary>
    public PhilippineDlCode[] DlCodes { get; set; } = [];
    public string IssuingCountry { get; set; } = null!;
    public string? IssuingRegion { get; set; }
    public DateTime IssuedDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? MedicalCertExpiresAt { get; set; }
    public Guid? DocumentId { get; set; }
}
