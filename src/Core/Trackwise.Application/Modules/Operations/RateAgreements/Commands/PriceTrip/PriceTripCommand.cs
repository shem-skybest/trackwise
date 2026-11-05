using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>
/// Prices a trip from its client's rate agreement and writes the charge onto its drops. Running it
/// again re-prices, e.g. after a drop is added or the truck changes.
/// </summary>
public class PriceTripCommand : ICommand<Result<TripRateQuoteDto>>
{
    public Guid TripId { get; set; }

    /// <summary>The trip's local date. Omitted, it is read from the loads' pickup dates (UTC).</summary>
    public DateOnly? RatedOn { get; set; }
}
