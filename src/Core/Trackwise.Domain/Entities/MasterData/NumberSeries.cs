using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A per-series counter behind generated master-data codes, with the pattern each entity formats
/// its codes with.
/// <para>
/// Allocation must be a single atomic statement, never read-then-write: two stewards saving in the
/// same second would otherwise be handed the same code, and the unique index on the entity's
/// <c>Code</c> would fail the second one at save time. That atomicity - not a held reservation - is
/// what the brief's "reservation mechanism" actually requires; gaps from abandoned edits are
/// accepted, collisions are not.
/// </para>
/// <para>
/// Modelled on <see cref="WorkfileNumberSeries"/>, which solves the same problem for workfile
/// references but hard-codes its format. Folding that entity into this one needs a migration of
/// live counters, so it is deliberately left for a follow-up rather than ridden along here.
/// </para>
/// </summary>
public class NumberSeries : AuditableEntity, ITenantEntity
{
    /// <summary>Series key, unique per tenant - conventionally the entity name, e.g. <c>Branch</c>.</summary>
    public required string Series { get; set; }

    /// <summary>Literal prefix, e.g. <c>BR</c>. Empty for a bare running number.</summary>
    public string Prefix { get; set; } = "";

    /// <summary>Separator between prefix and number; blank produces <c>BR000001</c>.</summary>
    public string Separator { get; set; } = "-";

    /// <summary>Zero-padding width for the running number.</summary>
    public int PadWidth { get; set; } = 6;

    /// <summary>The highest number handed out so far; 0 before the first allocation.</summary>
    public long LastAllocated { get; set; }

    /// <summary>Formats an allocated number into its display code, e.g. "BR-000123".</summary>
    public string Format(long value) =>
        $"{Prefix}{(Prefix.Length > 0 ? Separator : "")}{value.ToString().PadLeft(PadWidth, '0')}";
}
