using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities.Safety;

/// <summary>
/// Someone who saw the incident
/// </summary>
public class IncidentWitness : Entity, ITenantEntity
{
    public Guid IncidentId { get; set; }
    public virtual Incident Incident { get; set; } = null!;

    public required string Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Statement { get; set; }
}
