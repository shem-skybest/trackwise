/**
 * Display helpers for the Booking module. Enum values arrive from the generated client as
 * snake_case, camelCase or PascalCase depending on the serializer, so everything here is
 * written to tolerate all three instead of hard-coding one spelling.
 */

/** 'in_progress' | 'inProgress' | 'InProgress' -> 'inprogress', for safe comparisons. */
export function statusKey(value: unknown): string {
  return String(value ?? "")
    .replace(/[\s_-]/g, "")
    .toLowerCase();
}

/** 'in_progress' | 'inProgress' -> 'In Progress'. Null/undefined -> '-'. */
export function humanize(value: unknown): string {
  if (value === null || value === undefined || value === "") {
    return "-";
  }
  return String(value)
    .replace(/_/g, " ")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

const ACRONYMS: Record<string, string> = {
  kyc: "KYC",
  gdp: "GDP",
  pti: "PTI",
};

function withAcronyms(text: string): string {
  return text.replace(/\b[A-Za-z]+\b/g, (word) => ACRONYMS[word.toLowerCase()] ?? word);
}

export function bookingStatusLabel(status: unknown): string {
  return humanize(status);
}

export function creditCheckLabel(status: unknown): string {
  return statusKey(status) === "notrequired" ? "Not required" : humanize(status);
}

export function documentTypeLabel(type: unknown): string {
  return withAcronyms(humanize(type));
}

export function documentStatusLabel(status: unknown): string {
  return humanize(status);
}

export function chargeSourceLabel(source: unknown): string {
  return statusKey(source) === "quoted" ? "Quoted" : "Added";
}
