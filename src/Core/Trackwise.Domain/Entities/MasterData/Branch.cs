using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// An office or branch of the company. The scoping entity for the rest of the module: most master
/// records belong to one branch, some are global.
/// <para>
/// Scoping is modelled by pointing a record at a branch - never by duplicating the record per
/// branch, which is one of the ways the legacy systems ended up with the drift this module exists
/// to remove. A null branch on a scoped record means "applies to every branch".
/// </para>
/// </summary>
public class Branch : AuditableEntity, ITenantEntity, IMasterDataRecord
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.ReferenceList;

    /// <inheritdoc />
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a branch code on write. Callers querying by code must normalise the search
    /// term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    /// <summary>Display abbreviation for dense grids, e.g. "MNL".</summary>
    public string? ShortName { get; set; }

    public Address? Address { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>TIN, when the branch bills under its own registration.</summary>
    public string? TaxId { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
