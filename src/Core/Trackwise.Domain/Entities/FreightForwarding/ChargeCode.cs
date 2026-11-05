using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents a charge code used in freight forwarding (e.g., OFR, BAF, DEM).
/// </summary>
public class ChargeCode : Entity, ITenantEntity
{
    public string Code { get; private set; }                     // e.g., "OFR", "BAF", "DEM"
    public string Name { get; private set; }                     // e.g., "Ocean Freight", "Bunker Adjustment Factor"
    public string? Description { get; private set; }
    public ChargeCategory Category { get; private set; }         // Base, Accessorial, Tax, Surcharge, Discount
    public FreightMode[] ApplicableModes { get; private set; }   // Domestic / Import / Export
    public string DefaultUnit { get; private set; }              // per shipment, per kg, per cbm, etc.
    public bool RequiresApproval { get; private set; }           // margin check needed
    public string? TaxCode { get; private set; }                 // for tax engine (if applicable)
    public bool IsActive { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<TariffRate> _rates = new();
    public virtual IReadOnlyCollection<TariffRate> Rates => _rates.AsReadOnly();

    public string? Currency { get; set; }

    /// <summary>
    /// Creates a new charge code.
    /// </summary>
    public static ChargeCode Create(string code, string name, ChargeCategory category,
        FreightMode[] modes, string defaultUnit, bool requiresApproval = false, string? description = null,
        string? taxCode = null)
    {
        var cc = new ChargeCode
        {
            Code = code.ToUpperInvariant(),
            Name = name,
            Description = description,
            Category = category,
            ApplicableModes = modes,
            DefaultUnit = defaultUnit,
            RequiresApproval = requiresApproval,
            TaxCode = taxCode,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        cc.DomainEvents.Add(new ChargeCodeCreated(cc.Id, code, name));
        return cc;
    }

    public void Deactivate()
    {
        IsActive = false;
        DomainEvents.Add(new ChargeCodeDeactivated(Id, Code));
    }

    public void UpdateDetails(string? name, string? description, ChargeCategory? category,
        FreightMode[]? modes, string? defaultUnit, bool? requiresApproval, string? taxCode)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (description != null) Description = description;
        if (category.HasValue) Category = category.Value;
        if (modes != null) ApplicableModes = modes;
        if (!string.IsNullOrWhiteSpace(defaultUnit)) DefaultUnit = defaultUnit;
        if (requiresApproval.HasValue) RequiresApproval = requiresApproval.Value;
        if (taxCode != null) TaxCode = taxCode;
        DomainEvents.Add(new ChargeCodeUpdated(Id, Code));
    }
}
// Domain events
public record ChargeCodeCreated(Guid Id, string Code, string Name) : IDomainEvent;
public record ChargeCodeUpdated(Guid Id, string Code) : IDomainEvent;
public record ChargeCodeDeactivated(Guid Id, string Code) : IDomainEvent;
