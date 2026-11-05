using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A physical container in the register. It moves across many loads and workfiles over its
/// lifecycle (empty pickup → loaded → in transit → delivered → returned); what is true of it on one
/// shipment is a <see cref="WorkfileContainer"/>.
/// </summary>
public partial class Container : AuditableEntity, ITenantEntity
{
    private string number = "";

    /// <summary>
    /// ISO 6346 container number - 4 letters (owner + category) + 7 digits.
    /// <para>
    /// Canonicalised on write by <see cref="NormalizeNumber"/>, so the unique index cannot be
    /// defeated by case and lookups can compare exactly instead of wrapping the column in
    /// <c>upper()</c> (which no index can serve).
    /// </para>
    /// </summary>
    public required string Number
    {
        get => number;
        set => number = NormalizeNumber(value);
    }

    /// <summary>
    /// The canonical form of a container number. Callers that query by number must normalise the
    /// search term through this, since the stored value is always canonical.
    /// </summary>
    public static string NormalizeNumber(string? value) =>
        value?.Trim().ToUpperInvariant() ?? "";

    /// <summary>
    /// The kind of box, from the master container-type list. Required: a container whose type
    /// nobody recorded cannot be planned around, stowed or priced.
    /// </summary>
    public required Guid ContainerTypeId { get; set; }

    public virtual ContainerType? ContainerType { get; set; }

    // Seal, weight, booking and B/L are facts of one move, not of the box, and live on
    // WorkfileContainer - the same box comes back on later shipments with new ones.

    /// <summary>
    /// True when the container is loaded with cargo, false when empty.
    /// </summary>
    public bool IsLaden { get; set; }

    public ContainerStatus Status { get; private set; } = ContainerStatus.Empty;

    public Guid? CurrentTerminalId { get; set; }
    public virtual Terminal? CurrentTerminal { get; set; }

    public string? Notes { get; set; }

    public DateTime? LoadedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
}
