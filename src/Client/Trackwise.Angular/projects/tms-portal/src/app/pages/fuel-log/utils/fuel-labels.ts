import type { ExpenseStatus, FleetCard } from "@trackwise/shared/api";
import type { UiBadgeIntent } from "@trackwise/shared/ui";

const REVIEW: Record<ExpenseStatus, { label: string; intent: UiBadgeIntent }> = {
  pending: { label: "For review", intent: "warn" },
  approved: { label: "Verified", intent: "success" },
  rejected: { label: "Flagged", intent: "danger" },
  paid: { label: "Paid", intent: "success" },
};

const CARD: Record<FleetCard, string> = {
  shell: "Shell",
  petron: "Petron",
  unioil: "Unioil",
};

export function reviewLabel(status: ExpenseStatus | undefined): string {
  return status ? REVIEW[status].label : "";
}

export function reviewIntent(status: ExpenseStatus | undefined): UiBadgeIntent {
  return status ? REVIEW[status].intent : "info";
}

/** No card means it was paid some other way, usually cash. */
export function cardLabel(card: FleetCard | null | undefined): string {
  return card ? CARD[card] : "Cash / other";
}
