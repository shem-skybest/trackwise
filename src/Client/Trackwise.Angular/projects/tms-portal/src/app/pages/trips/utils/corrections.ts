import type { TripCorrectionDto, TripCorrectionField } from "@trackwise/shared/api";
import { describeTime } from "./report-times";

const LABELS: Record<TripCorrectionField, string> = {
  arrived_at: "Arrival",
  temperature_at_arrival: "Temp at arrival",
  handling_started_at: "Start handling",
  handling_finished_at: "Done handling",
  departed_at: "Departure",
  temperature_at_departure: "Temp at departure",
  outcome: "Outcome",
  returned_boxes: "Boxes back",
  returned_bags: "Bags back",
  return_reason: "Why it came back",
  outcome_note: "Remarks",
  garage_departed_at: "Garage departure",
  garage_arrived_at: "Garage arrival",
};

const TIME_FIELDS = new Set<TripCorrectionField>([
  "arrived_at",
  "handling_started_at",
  "handling_finished_at",
  "departed_at",
  "garage_departed_at",
  "garage_arrived_at",
]);

const ENUM_FIELDS = new Set<TripCorrectionField>(["outcome", "return_reason"]);

/** One line per changed value: "Arrival: Tue 23 Sep, 08:00 → Tue 23 Sep, 23:29". */
export function describeChange(change: TripCorrectionDto): string {
  const field = change.field!;
  return `${LABELS[field]}: ${formatValue(field, change.oldValue)} → ${formatValue(field, change.newValue)}`;
}

/** Corrections saved together share a reason and a moment - one group per save, newest first. */
export interface CorrectionGroup {
  correctedAt: string;
  correctedBy: string | null;
  reason: string;
  changes: TripCorrectionDto[];
}

export function groupCorrections(corrections: readonly TripCorrectionDto[]): CorrectionGroup[] {
  const groups = new Map<string, CorrectionGroup>();
  for (const c of corrections) {
    const key = `${c.correctedAt}|${c.correctedBy ?? ""}|${c.reason ?? ""}`;
    const group = groups.get(key) ?? {
      correctedAt: c.correctedAt!,
      correctedBy: c.correctedBy ?? null,
      reason: c.reason ?? "",
      changes: [],
    };
    group.changes.push(c);
    groups.set(key, group);
  }
  return [...groups.values()];
}

function formatValue(field: TripCorrectionField, value: string | null | undefined): string {
  if (value == null || value === "") {
    return "blank";
  }
  if (TIME_FIELDS.has(field)) {
    return describeTime(new Date(value));
  }
  if (ENUM_FIELDS.has(field)) {
    // Stored by the enum member's name, e.g. "FoulTrip" or "WrongPoOrBarcode".
    return value.replace(/([a-z])([A-Z])/g, "$1 $2");
  }
  if (field === "temperature_at_arrival" || field === "temperature_at_departure") {
    return `${value} °C`;
  }
  return value;
}
