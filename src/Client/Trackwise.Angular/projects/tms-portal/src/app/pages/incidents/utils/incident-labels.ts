import type {
  AccidentType,
  IncidentKind,
  IncidentSeverity,
  IncidentStatus,
} from "@trackwise/shared/api";
import {
  accidentTypeOptions,
  incidentKindOptions,
  incidentSeverityOptions,
} from "@trackwise/shared/api/enums";
import type { UiBadgeIntent } from "@trackwise/shared/ui";

const STATUS: Record<IncidentStatus, { label: string; intent: UiBadgeIntent }> = {
  open: { label: "Open", intent: "warn" },
  under_review: { label: "Under review", intent: "info" },
  resolved: { label: "Resolved", intent: "success" },
  closed: { label: "Closed", intent: "secondary" },
};

const SEVERITY_INTENT: Record<IncidentSeverity, UiBadgeIntent> = {
  minor: "secondary",
  moderate: "warn",
  severe: "danger",
  fatal: "danger",
};

const KIND_SHORT: Partial<Record<IncidentKind, string>> = {
  product_damage: "Product damage",
  loading_error: "Loading error",
  documents: "Documents",
  theft: "Theft / missing",
  backload: "Backload",
  delay: "Delay",
  cancellation: "Cancellation",
};

/** The kind as a short label - the option lists carry the longer explanation. */
export function incidentKindLabel(kind: IncidentKind | undefined): string {
  if (!kind) return "";
  return KIND_SHORT[kind] ?? incidentKindOptions.find((o) => o.value === kind)?.label ?? kind;
}

export function incidentStatusLabel(status: IncidentStatus | undefined): string {
  return status ? STATUS[status].label : "";
}

export function incidentStatusIntent(status: IncidentStatus | undefined): UiBadgeIntent {
  return status ? STATUS[status].intent : "secondary";
}

export function incidentSeverityLabel(severity: IncidentSeverity | undefined): string {
  const label = incidentSeverityOptions.find((o) => o.value === severity)?.label ?? "";
  return label.split(" - ")[0];
}

export function incidentSeverityIntent(severity: IncidentSeverity | undefined): UiBadgeIntent {
  return severity ? SEVERITY_INTENT[severity] : "secondary";
}

export function accidentTypeLabel(type: AccidentType | null | undefined): string {
  return accidentTypeOptions.find((o) => o.value === type)?.label ?? "";
}
