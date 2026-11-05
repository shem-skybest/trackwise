using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>What one load of a truck leg did to one box (or to loose cargo, with no slot).</summary>
internal sealed record LegMove(
    Guid LegId,
    string LegReference,
    TruckLegRole Role,
    Guid? SlotId,
    long? TripNumber,
    string? From,
    bool FromIsPort,
    string? To,
    bool ToIsPort,
    DateTime? PlannedPickup,
    DateTime? PickupArrived,
    DateTime? PickupDeparted,
    DateTime? DropArrived,
    DateTime? DropDeparted);

/// <summary>The sea leg as staff typed it (FORWARDING_LEGS_PLAN.md F5: no carrier feed yet).</summary>
internal sealed record VesselDates(DateTime? Etd, DateTime? Eta, string? Origin, string? Destination);

/// <summary>
/// Builds a forwarding workfile's door-to-door timeline and each box's status from the vessel dates
/// and its legs' trip events. Worked out on every read, never stored, so a corrected trip time or a
/// moved ETA shows up without anything to keep in step.
/// <para>
/// Steps sort by where they sit in the move before when they happened: legs that bring a box to the
/// port, then the vessel, then legs that take it away. A stale ETA (staff forgot to move it) then
/// can't put a box that a truck already delivered back "at port".
/// </para>
/// </summary>
internal static class ContainerTimeline
{
    private const int BeforeVessel = 0;
    private const int Vessel = 1;
    private const int AfterVessel = 2;

    public static List<WorkfileTimelineEntryDto> Build(
        VesselDates vessel,
        IReadOnlyList<LegMove> moves,
        List<WorkfileContainerDto> containers,
        DateTime now)
    {
        var vesselSteps = VesselSteps(vessel).ToList();
        var moveSteps = moves.SelectMany(MoveSteps).ToList();

        foreach (var container in containers)
        {
            var last = vesselSteps
                .Concat(moveSteps.Where(s => s.Move!.SlotId == container.Id))
                .Where(s => s.Kind != WorkfileTimelineEventKind.PickupPlanned && s.At <= now)
                .OrderBy(s => s.Phase).ThenBy(s => s.At).ThenBy(s => s.Kind)
                .LastOrDefault();

            if (last is not null)
            {
                (container.MoveStatus, container.MoveStatusPlace) = StatusAfter(last, vessel);
                container.MoveStatusSince = last.At;
            }
        }

        var labels = WorkfileContainerReader.Labels(containers);
        var truckEntries = moveSteps
            .GroupBy(s => (s.Kind, s.Move!.LegId, s.At, s.Move.TripNumber))
            .Select(g =>
            {
                var step = g.First();
                var entry = ToEntry(step, now);
                entry.LegId = step.Move!.LegId;
                entry.LegReferenceNumber = step.Move.LegReference;
                entry.TripNumber = step.Move.TripNumber;
                entry.Containers = g
                    .Select(s => s.Move!.SlotId is { } slotId ? labels.GetValueOrDefault(slotId) : null)
                    .OfType<string>()
                    .Distinct()
                    .ToList();
                return (step, entry);
            });

        return vesselSteps.Select(s => (step: s, entry: ToEntry(s, now)))
            .Concat(truckEntries)
            .OrderBy(x => x.step.Phase).ThenBy(x => x.step.At).ThenBy(x => x.step.Kind)
            .Select(x => x.entry)
            .ToList();
    }

    private static IEnumerable<Step> VesselSteps(VesselDates vessel)
    {
        if (vessel.Etd is { } etd)
        {
            yield return new Step(WorkfileTimelineEventKind.VesselDeparted, Vessel, etd, true, vessel.Origin, null);
        }

        if (vessel.Eta is { } eta)
        {
            yield return new Step(WorkfileTimelineEventKind.VesselArrived, Vessel, eta, true, vessel.Destination, null);
        }
    }

    private static IEnumerable<Step> MoveSteps(LegMove move)
    {
        var phase = PhaseOf(move);

        if (move.PickupArrived is null && move.PlannedPickup is { } planned)
        {
            yield return new Step(WorkfileTimelineEventKind.PickupPlanned, phase, planned, true, move.From, move);
        }

        if (move.PickupArrived is { } pickupArrived)
        {
            yield return new Step(WorkfileTimelineEventKind.TruckArrivedAtPickup, phase, pickupArrived, false, move.From, move);
        }

        if (move.PickupDeparted is { } pickupDeparted)
        {
            yield return new Step(WorkfileTimelineEventKind.TruckLeftPickup, phase, pickupDeparted, false, move.From, move);
        }

        if (move.DropArrived is { } dropArrived)
        {
            yield return new Step(WorkfileTimelineEventKind.TruckArrivedAtDrop, phase, dropArrived, false, move.To, move);
        }

        if (move.DropDeparted is { } dropDeparted)
        {
            yield return new Step(WorkfileTimelineEventKind.TruckLeftDrop, phase, dropDeparted, false, move.To, move);
        }
    }

    /// <summary>
    /// A pickup leg feeds the vessel and a delivery leg follows it. A transfer that starts at a port
    /// is clearing a landed box; one that doesn't is staging a box for the vessel.
    /// </summary>
    private static int PhaseOf(LegMove move) => move.Role switch
    {
        TruckLegRole.Pickup => BeforeVessel,
        TruckLegRole.Delivery => AfterVessel,
        _ => move.FromIsPort ? AfterVessel : BeforeVessel
    };

    private static (ContainerMoveStatus, string?) StatusAfter(Step step, VesselDates vessel) => step.Kind switch
    {
        WorkfileTimelineEventKind.VesselDeparted => (ContainerMoveStatus.OnVessel, vessel.Destination),
        WorkfileTimelineEventKind.VesselArrived => (ContainerMoveStatus.AtPort, vessel.Destination),
        WorkfileTimelineEventKind.TruckArrivedAtPickup => (ContainerMoveStatus.Loading, step.Move!.From),
        WorkfileTimelineEventKind.TruckLeftPickup => (ContainerMoveStatus.OnTheRoad, step.Move!.To),
        WorkfileTimelineEventKind.TruckArrivedAtDrop => (ContainerMoveStatus.Unloading, step.Move!.To),
        WorkfileTimelineEventKind.TruckLeftDrop => (DroppedStatus(step.Move!), step.Move!.To),
        _ => (ContainerMoveStatus.NotStarted, null)
    };

    private static ContainerMoveStatus DroppedStatus(LegMove move) =>
        move.ToIsPort ? ContainerMoveStatus.AtPort
        : move.Role == TruckLegRole.Delivery ? ContainerMoveStatus.Delivered
        : ContainerMoveStatus.AtYard;

    private static WorkfileTimelineEntryDto ToEntry(Step step, DateTime now) => new()
    {
        Kind = step.Kind,
        At = step.At,
        IsEstimate = step.IsEstimate,
        IsUpcoming = step.At > now,
        Place = step.Place
    };

    private sealed record Step(
        WorkfileTimelineEventKind Kind, int Phase, DateTime At, bool IsEstimate, string? Place, LegMove? Move);
}
