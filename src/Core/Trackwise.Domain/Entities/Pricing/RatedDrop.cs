using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>One drop on the trip being priced.</summary>
/// <param name="DropKey">Same key = same place; a second load to one outlet is not an extra drop.</param>
/// <param name="Refused">The drop ended as a foul trip or was cancelled by the client on the day.</param>
public sealed record RatedDrop(
    Guid LoadId, string DropKey, string? City, string? Province, double? DistanceKm, bool Refused = false);
