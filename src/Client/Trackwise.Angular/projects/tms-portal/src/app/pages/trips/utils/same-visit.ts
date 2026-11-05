import type { TripStopDto } from "@trackwise/shared/api";

/** Pickups closer than this (in degrees, ~100 m) are one warehouse visit - mirrors the API's rule. */
const SAME_VISIT_DEGREES = 0.001;

/**
 * The pickups in `stops` that make up one warehouse visit with `stop`, itself included. A drop is
 * its own visit. The API applies a pickup's events to the same group.
 */
export function sameVisitPickups(stop: TripStopDto, stops: readonly TripStopDto[]): TripStopDto[] {
  if (stop.type !== "pick_up") {
    return [stop];
  }

  return stops.filter(
    (s) =>
      s.type === "pick_up" &&
      Math.abs((s.location.latitude ?? 0) - (stop.location.latitude ?? 0)) <= SAME_VISIT_DEGREES &&
      Math.abs((s.location.longitude ?? 0) - (stop.location.longitude ?? 0)) <= SAME_VISIT_DEGREES,
  );
}
