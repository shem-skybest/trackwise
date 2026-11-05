using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.DbMigrator.Models;

/// <summary>
/// A client's delivery outlet, merged from the stops on Monday's 2026 CCL DAILY DELIVERY
/// MONITORING board (spelling variants folded together), seeded by <c>OutletSeeder</c>. A few are
/// the client's pickup warehouses instead, taken from nSFS.
/// </summary>
public record OutletData
{
    /// <summary><c>{client tag}-{NNNN}</c>, e.g. <c>HFI-0003</c>. Outside the <c>LOC-</c> number series.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Exact name of a seeded customer.</summary>
    public required string Customer { get; set; }

    /// <summary><c>Warehouse</c> for a pickup warehouse; otherwise a delivery point.</summary>
    public LocationType Type { get; set; } = LocationType.DeliveryPoint;

    /// <summary>Where the record came from, when not the Monday delivery board.</summary>
    public string? Source { get; set; }

    /// <summary>Most common Monday cut-off, 24-hour <c>HH:mm</c>.</summary>
    public string? DefaultCutoff { get; set; }

    /// <summary>Null for the handful of outlets Monday never pinned inside the Philippines.</summary>
    public Address? Address { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>True when the best pin is only Google's city centre, not the outlet itself.</summary>
    public bool CityLevelPin { get; set; }

    public int Stops { get; set; }
    public string? FirstDelivery { get; set; }
    public string? LastDelivery { get; set; }

    /// <summary>Other spellings the board used for the same outlet.</summary>
    public string[] Aliases { get; set; } = [];
}
