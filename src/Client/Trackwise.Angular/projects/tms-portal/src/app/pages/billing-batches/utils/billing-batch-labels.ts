import type {
  BillingBatchStatus,
  BillingBatchTripDto,
  BillingChargeKind,
  BillingCycle,
} from "@trackwise/shared/api";
import type { UiBadgeIntent } from "@trackwise/shared/ui";

const STATUS: Record<BillingBatchStatus, { label: string; intent: UiBadgeIntent }> = {
  draft: { label: "Draft", intent: "secondary" },
  locked: { label: "Locked", intent: "info" },
  forwarded: { label: "With Accounting", intent: "warn" },
  billed: { label: "Billed", intent: "success" },
  cancelled: { label: "Cancelled", intent: "danger" },
};

const KIND: Record<BillingChargeKind, string> = {
  demurrage: "Demurrage",
  backload: "Backload",
  different_location: "Different location",
  tolls_and_parking: "Tolls and parking",
  other: "Other",
};

const CYCLE: Record<BillingCycle, string> = {
  semi_monthly: "Twice a month",
  weekly: "Weekly",
  per_trip: "Per trip",
  monthly: "Monthly",
};

export function batchStatusLabel(status: BillingBatchStatus | undefined): string {
  return status ? STATUS[status].label : "";
}

export function batchStatusIntent(status: BillingBatchStatus | undefined): UiBadgeIntent {
  return status ? STATUS[status].intent : "secondary";
}

export function chargeKindLabel(kind: BillingChargeKind | undefined): string {
  return kind ? KIND[kind] : "";
}

export function billingCycleLabel(cycle: BillingCycle | null | undefined): string {
  return cycle ? CYCLE[cycle] : "No billing cycle";
}

/** "Delivered", or what went otherwise: "1 foul trip, 2 partial". */
export function tripOutcome(trip: BillingBatchTripDto): string {
  const parts: string[] = [];
  if (trip.foulTripDrops) {
    parts.push(`${trip.foulTripDrops} foul trip`);
  }
  if (trip.cancelledDrops) {
    parts.push(`${trip.cancelledDrops} cancelled by client`);
  }
  if (trip.partialDrops) {
    parts.push(`${trip.partialDrops} partial`);
  }
  return parts.length ? parts.join(", ") : "Delivered";
}
