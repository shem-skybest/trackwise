import type { CashExpenseCategory, CashExpenseStatus, CashFundStatus } from "@trackwise/shared/api";
import type { UiBadgeIntent } from "@trackwise/shared/ui";

const FUND_STATUS: Record<CashFundStatus, { label: string; intent: UiBadgeIntent }> = {
  open: { label: "Open", intent: "info" },
  liquidated: { label: "Liquidated", intent: "success" },
};

const EXPENSE_STATUS: Record<CashExpenseStatus, { label: string; intent: UiBadgeIntent }> = {
  with_ops: { label: "With Ops", intent: "warn" },
  forwarded: { label: "Forwarded", intent: "info" },
  checked: { label: "Checked", intent: "success" },
  disallowed: { label: "Disallowed", intent: "danger" },
};

const CATEGORY: Record<CashExpenseCategory, string> = {
  passway: "Passway",
  parking: "Parking",
  toll: "Toll",
  courier: "Courier",
  other: "Other",
};

export function fundStatusLabel(status: CashFundStatus | undefined): string {
  return status ? FUND_STATUS[status].label : "";
}

export function fundStatusIntent(status: CashFundStatus | undefined): UiBadgeIntent {
  return status ? FUND_STATUS[status].intent : "secondary";
}

export function expenseStatusLabel(status: CashExpenseStatus | undefined): string {
  return status ? EXPENSE_STATUS[status].label : "";
}

export function expenseStatusIntent(status: CashExpenseStatus | undefined): UiBadgeIntent {
  return status ? EXPENSE_STATUS[status].intent : "secondary";
}

export function categoryLabel(category: CashExpenseCategory | undefined): string {
  return category ? CATEGORY[category] : "";
}
