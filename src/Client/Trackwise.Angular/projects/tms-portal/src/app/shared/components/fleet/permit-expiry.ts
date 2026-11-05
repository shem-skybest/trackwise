import type { VehiclePermitDto } from "@trackwise/shared/api";
import { vehiclePermitTypeOptions } from "@trackwise/shared/api/enums";
import type { UiBadgeIntent } from "@trackwise/shared/ui";

export interface PermitExpiry {
  label: string;
  intent: UiBadgeIntent;
}

/**
 * How urgent a paper's expiry is, on the same 60/30/7-day steps the reminder digest uses. A
 * superseded paper - an older one of a type that has since been renewed - is history, not a warning.
 */
export function permitExpiry(permit: VehiclePermitDto): PermitExpiry {
  const days = permit.daysUntilExpiry ?? 0;
  if (!permit.isCurrent) {
    return { label: "Superseded", intent: "secondary" };
  }
  if (days < 0) {
    return { label: `Expired ${-days}d ago`, intent: "danger" };
  }
  if (days === 0) {
    return { label: "Expires today", intent: "danger" };
  }
  if (days <= 30) {
    return { label: `Expires in ${days}d`, intent: days <= 7 ? "danger" : "warn" };
  }
  if (days <= 60) {
    return { label: `Expires in ${days}d`, intent: "info" };
  }
  return { label: "Valid", intent: "success" };
}

export function permitTypeLabel(permit: VehiclePermitDto): string {
  return vehiclePermitTypeOptions.find((o) => o.value === permit.type)?.label ?? permit.type ?? "";
}
