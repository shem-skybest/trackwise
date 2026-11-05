using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One planned drop on a Cold Chain booking: a saved client delivery location and what goes there.
/// Becomes one Load when the booking is turned into a trip.
/// </summary>
public class BookingDrop : Entity, ITenantEntity
{
    public Guid BookingId { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>The client's saved delivery location (a <see cref="Location"/>).</summary>
    public Guid SiteId { get; private set; }

    public int? PlannedBoxes { get; private set; }
    public int? PlannedBags { get; private set; }
    public string? Notes { get; private set; }

    internal static BookingDrop Create(Guid bookingId, int sortOrder, BookingDropSpec spec)
    {
        if (spec.PlannedBoxes < 0 || spec.PlannedBags < 0)
        {
            throw new InvalidOperationException("Boxes and bags can't be negative.");
        }

        return new BookingDrop
        {
            BookingId = bookingId,
            SortOrder = sortOrder,
            SiteId = spec.SiteId,
            PlannedBoxes = spec.PlannedBoxes,
            PlannedBags = spec.PlannedBags,
            Notes = string.IsNullOrWhiteSpace(spec.Notes) ? null : spec.Notes.Trim()
        };
    }
}
