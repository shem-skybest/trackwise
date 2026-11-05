import type { BillingCycle } from "@trackwise/shared/api";

export interface Period {
  start: Date;
  end: Date;
}

/**
 * The client's billing period that `day` falls in, cut the way the server cuts it
 * (`BillingPeriod.For`): 1-15 / 16-end, Monday-Saturday (a Sunday closes its own week), the month,
 * or the day alone.
 */
export function periodFor(cycle: BillingCycle, day: Date): Period {
  const year = day.getFullYear();
  const month = day.getMonth();
  switch (cycle) {
    case "semi_monthly":
      return day.getDate() <= 15
        ? { start: new Date(year, month, 1), end: new Date(year, month, 15) }
        : { start: new Date(year, month, 16), end: new Date(year, month + 1, 0) };
    case "weekly": {
      const monday = new Date(year, month, day.getDate() - ((day.getDay() + 6) % 7));
      const end =
        day.getDay() === 0
          ? day
          : new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 5);
      return { start: monday, end };
    }
    case "monthly":
      return { start: new Date(year, month, 1), end: new Date(year, month + 1, 0) };
    default:
      return { start: day, end: day };
  }
}

/** The last period that has fully ended - what is normally billed next. */
export function lastClosedPeriod(cycle: BillingCycle, today = new Date()): Period {
  const current = periodFor(cycle, today);
  const dayBefore = new Date(
    current.start.getFullYear(),
    current.start.getMonth(),
    current.start.getDate() - 1,
  );
  return cycle === "per_trip" ? current : periodFor(cycle, dayBefore);
}
