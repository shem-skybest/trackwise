/** The API refuses a longer run; about two months of daily bookings. */
export const MAX_REPEAT_DAYS = 62;

/** Every day from `from` to `until`, both included, that falls on one of `weekdays` (0 = Sunday). */
export function standingOrderDates(from: Date, until: Date, weekdays: readonly number[]): Date[] {
  const dates: Date[] = [];
  const day = new Date(from.getFullYear(), from.getMonth(), from.getDate());
  const last = new Date(until.getFullYear(), until.getMonth(), until.getDate());

  // Capped past the API limit so a far-off "until" can't spin; the caller reports the overflow.
  while (day <= last && dates.length <= MAX_REPEAT_DAYS) {
    if (weekdays.includes(day.getDay())) {
      dates.push(new Date(day));
    }
    day.setDate(day.getDate() + 1);
  }

  return dates;
}
