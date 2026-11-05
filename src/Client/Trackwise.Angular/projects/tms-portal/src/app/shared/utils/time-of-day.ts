/**
 * Helpers for the API's time-of-day strings (a C# `TimeOnly`, serialized "HH:mm:ss"), e.g. a
 * delivery location's default cut-off.
 */

/** "10:00:00" -> "10:00 AM"; null/blank -> "". */
export function formatTimeOfDay(value: string | null | undefined): string {
  const date = timeOfDayToDate(value);
  return date ? date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" }) : "";
}

/** "10:00:00" -> a Date today at 10:00, for a time-only `ui-date-field`. */
export function timeOfDayToDate(value: string | null | undefined): Date | null {
  const match = /^(\d{1,2}):(\d{2})/.exec(value ?? "");
  if (!match) {
    return null;
  }
  const date = new Date();
  date.setHours(Number(match[1]), Number(match[2]), 0, 0);
  return date;
}

/** A time-only `ui-date-field` value -> "HH:mm:00"; null stays null. */
export function dateToTimeOfDay(value: Date | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${pad(value.getHours())}:${pad(value.getMinutes())}:00`;
}

/**
 * `day` at the time of day `value`, or null when `value` is blank. The date part comes from `day`
 * (or today) so a cut-off lands on the delivery date rather than the day the form was opened.
 */
export function atTimeOfDay(day: Date | null, value: string | null | undefined): Date | null {
  const time = timeOfDayToDate(value);
  if (!time) {
    return null;
  }
  const result = new Date(day ?? new Date());
  result.setHours(time.getHours(), time.getMinutes(), 0, 0);
  return result;
}
