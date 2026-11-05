using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The diesel price per litre that fuel surcharges are read against, from a date until the next
/// entry. Effective-dated so re-pricing an old trip uses the diesel price of its day, not today's.
/// </summary>
public class DieselPrice : AuditableEntity, ITenantEntity
{
    public DateOnly EffectiveFrom { get; set; }

    public decimal PricePerLitre { get; set; }

    /// <summary>Where the figure came from, e.g. "DOE weekly advisory, Sep 23".</summary>
    public string? Notes { get; set; }
}
