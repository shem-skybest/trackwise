/** An API `DateOnly` ("2026-09-21") as a local date, for a `ui-date-field`. */
export function isoToDate(value: string | null | undefined): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value ?? "");
  return match ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])) : null;
}

/** A local date as the API's `DateOnly` - never via `toISOString()`, which shifts it to UTC. */
export function dateToIso(value: Date | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`;
}
