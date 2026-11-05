using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

/// <summary>The leg side of a workfile: its parent's reference, its own legs, or a leg's loads.</summary>
public sealed record WorkfileLegsView(
    string? ParentReferenceNumber,
    List<TruckLegDto> Legs,
    List<TruckLegLoadDto> LegLoads);
