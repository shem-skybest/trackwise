using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities.Safety;

/// <summary>
/// Someone outside the company involved in the incident - the other vehicle, a property owner
/// </summary>
public class IncidentThirdParty : Entity, ITenantEntity
{
    public Guid IncidentId { get; set; }
    public virtual Incident Incident { get; set; } = null!;

    public required string Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? DriverLicense { get; set; }

    // Vehicle info
    public string? VehicleMake { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public string? VehicleLicensePlate { get; set; }
    public string? VehicleVin { get; set; }
    public string? VehicleColor { get; set; }

    // Insurance info
    public string? InsuranceCompany { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public string? InsuranceAgentPhone { get; set; }
}
