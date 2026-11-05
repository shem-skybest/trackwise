using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Commands;

public class CreateTenantCommand : ICommand
{
    public string Name { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? BillingEmail { get; set; }
    public string? DotNumber { get; set; }
    public required Address CompanyAddress { get; set; }

    /// <summary>Null falls back to <see cref="Domain.Primitives.Enums.OperatingMode.Fleet"/>.</summary>
    public OperatingMode? OperatingMode { get; set; }

    /// <summary>Sets the tenant's currency, units and time zone too. Null falls back to <see cref="Domain.Primitives.Enums.Region.US"/>.</summary>
    public Region? Region { get; set; }

    // Owner account created alongside the tenant
    public string OwnerEmail { get; set; } = null!;
    public string OwnerFirstName { get; set; } = null!;
    public string OwnerLastName { get; set; } = null!;
}
