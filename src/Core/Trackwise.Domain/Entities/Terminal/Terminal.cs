using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents an intermodal terminal - sea port, rail terminal, inland depot,
/// air cargo facility, or border crossing - used as a pickup / drop-off point.
/// <para>
/// Also the master-data record for a port: the canonical entry the other systems join on, rather
/// than the free-text origin and destination strings nSFS keeps today. It is extended here rather
/// than shadowed by a parallel <c>Port</c> entity, since a second copy would recreate exactly the
/// duplication the module exists to remove.
/// </para>
/// </summary>
public class Terminal : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    public MasterDataGroup Group => MasterDataGroup.ReferenceList;

    public required string Name { get; set; }

    private string code = "";

    /// <summary>
    /// UN/LOCODE identifier (e.g. "BEANR" Antwerp, "USLAX" Los Angeles, "DEHAM" Hamburg).
    /// <para>
    /// Canonicalised on write by <see cref="NormalizeCode"/> - see <c>Container.Number</c> for why.
    /// </para>
    /// <para>
    /// Immutable once the record exists. It is the stable handle other systems join on and it
    /// survives a merge, so renaming it would strand every legacy key pointing here. Not every
    /// legacy port carries a real UN/LOCODE, which is why the column is wider than five characters.
    /// </para>
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// The canonical form of a UN/LOCODE. Callers that query by code must normalise the search term
    /// through this, since the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) =>
        value?.Trim().ToUpperInvariant() ?? "";

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g. "BE", "US", "DE").
    /// </summary>
    public required string CountryCode { get; set; }

    public required TerminalType Type { get; set; }

    public required Address Address { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Owning branch, or null when every branch uses this port. A port one office maintains is
    /// still readable everywhere - scoping governs stewardship, not visibility.
    /// </summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the port is still offered for new work. Distinct from deletion: a port that stops
    /// being called keeps every load and container that ever referenced it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
