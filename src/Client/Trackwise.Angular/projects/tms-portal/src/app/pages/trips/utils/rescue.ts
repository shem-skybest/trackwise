import type { TripDto, TripRescueDto, TripStopDto } from "@trackwise/shared/api";

/**
 * A drop whose goods are on the truck - its pickup is recorded - that the trip's own truck hasn't
 * reached and no rescue has taken over yet.
 */
export function rescuableDrops(trip: TripDto): TripStopDto[] {
  const stops = trip.stops ?? [];
  const loaded = new Set(
    stops.filter((s) => s.type === "pick_up" && s.arrivedAt).map((s) => s.loadId),
  );
  return [...stops]
    .filter((s) => s.type === "drop_off" && !s.arrivedAt && !s.rescueId && loaded.has(s.loadId))
    .sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
}

/** A rescue can be recorded on a trip under way with a drop still to take over. */
export function canRescue(trip: TripDto): boolean {
  const underWay = trip.status === "dispatched" || trip.status === "in_transit";
  return underWay && rescuableDrops(trip).length > 0;
}

/** A rescue is part of the record once its truck has reached one of its drops. */
export function canUndoRescue(trip: TripDto, rescue: TripRescueDto): boolean {
  const stops = (trip.stops ?? []).filter((s) => s.rescueId === rescue.id);
  return !stops.some((s) => !!s.arrivedAt);
}

/** "Drop 3", the drop's position on the trip as the crew report numbers it. */
export function dropNumber(trip: TripDto, stopId: string | null | undefined): number {
  const drops = [...(trip.stops ?? [])]
    .filter((s) => s.type === "drop_off")
    .sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
  return drops.findIndex((s) => s.id === stopId) + 1;
}

/** "ABF3727 · Juan Cruz" - the rescue truck and its driver, for a drop the rescue delivered. */
export function rescueLabel(rescue: TripRescueDto): string {
  return [rescue.truckNumber, rescue.driverName].filter(Boolean).join(" · ");
}
