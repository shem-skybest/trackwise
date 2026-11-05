import type { TripDto, UpdateTripCommand } from "@trackwise/shared/api";
import { dateToIso } from "@/shared/utils";

/**
 * The truck and crew are the plan until the first arrival is recorded, then the record of who ran
 * the trip - the API refuses changes from there (`Trip.HasRecordedStops`), so the page doesn't offer them.
 */
export function isHeaderEditable(trip: TripDto): boolean {
  const modifiable = trip.status === "draft" || trip.status === "dispatched";
  return modifiable && !(trip.stops ?? []).some((s) => !!s.arrivedAt);
}

/** A drop can go while nothing is recorded on it or its pickup, and it isn't the trip's last. */
export function canRemoveDrop(trip: TripDto, loadId: string | null | undefined): boolean {
  if (!loadId || !(trip.status === "draft" || trip.status === "dispatched")) return false;
  const drops = (trip.stops ?? []).filter((s) => s.type === "drop_off");
  const ownStops = (trip.stops ?? []).filter((s) => s.loadId === loadId);
  return drops.length > 1 && !ownStops.some((s) => !!s.arrivedAt);
}

/**
 * The day the crew's availability is checked on - the trip's earliest requested pickup, as the API's
 * `TripDay` works it out, else today.
 */
export function crewDay(trip: TripDto): string {
  const pickups = (trip.loads ?? [])
    .map((l) => l.requestedPickupDate)
    .filter((d): d is string => !!d)
    .map((d) => new Date(d).getTime());
  return dateToIso(pickups.length ? new Date(Math.min(...pickups)) : new Date())!;
}

/**
 * An `UpdateTrip` that keeps the trip's current truck and crew unless overridden: the API reads an
 * omitted driver or helper as "no one", so they are always sent.
 */
export function headerUpdate(
  trip: TripDto,
  changes: Partial<UpdateTripCommand>,
): UpdateTripCommand {
  return {
    tripId: trip.id!,
    truckId: trip.truckId,
    driverId: trip.driverId,
    helperId: trip.helperId,
    secondHelperId: trip.secondHelperId,
    ...changes,
  };
}
