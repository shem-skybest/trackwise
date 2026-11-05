using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>What a rate line is made from - the shape commands, the importer and revisions share.</summary>
public sealed record RateLineSpec(
    string Label,
    TruckTonnageClass? Tonnage,
    decimal Amount,
    int? MinKm,
    int? MaxKm,
    string? City,
    string? Province,
    double? DistanceKm,
    decimal? FuelPercent,
    IReadOnlyList<(decimal DieselPrice, decimal Amount)>? FuelSurcharges);
