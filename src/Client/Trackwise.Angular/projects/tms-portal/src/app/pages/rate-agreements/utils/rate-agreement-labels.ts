import type {
  FuelAdjustmentMethod,
  RateAgreementStatus,
  TruckTonnageClass,
} from "@trackwise/shared/api";
import type { UiBadgeIntent } from "@trackwise/shared/ui";

const STATUS: Record<RateAgreementStatus, { label: string; intent: UiBadgeIntent }> = {
  draft: { label: "Draft", intent: "warn" },
  active: { label: "In force", intent: "success" },
  superseded: { label: "Superseded", intent: "secondary" },
  withdrawn: { label: "Withdrawn", intent: "danger" },
};

const TONNAGE: Record<TruckTonnageClass, string> = {
  two_tonner: "2T",
  three_tonner: "3T",
  four_tonner: "4T",
  ten_tonner: "10T",
};

const FUEL: Record<FuelAdjustmentMethod, string> = {
  none: "Fuel included",
  diesel_tier_surcharge: "Gas on top per diesel price",
  percentage: "Fuel % on top",
};

export function statusLabel(status: RateAgreementStatus | undefined): string {
  return status ? STATUS[status].label : "";
}

export function statusIntent(status: RateAgreementStatus | undefined): UiBadgeIntent {
  return status ? STATUS[status].intent : "secondary";
}

export function tonnageLabel(tonnage: TruckTonnageClass | null | undefined): string {
  return tonnage ? TONNAGE[tonnage] : "Any";
}

export function fuelMethodLabel(method: FuelAdjustmentMethod | undefined): string {
  return method ? FUEL[method] : "";
}
