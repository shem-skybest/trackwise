/**
 * Times as the crew report them: a clock time with no date ("ARRIVAL:2329H  START LOADING:0323H").
 * The date comes from the order of events - a trip runs forward, so a time earlier than the one
 * before it is the next day. That is how the office reads the message, and it keeps typing to the
 * four digits.
 */

/** A typed clock time: minutes past midnight, plus any days pushed on with trailing "+". */
export interface ClockTime {
  minutes: number;
  extraDays: number;
}

/** One link in the report's chain of times, in the order the trip ran. */
export interface ChainEntry {
  key: string;
  /** What the office typed; ignored when `fixed` is set. */
  text: string;
  /** A time already recorded - it anchors the chain and can't be retyped here. */
  fixed?: Date | null;
}

export type ResolvedTime = Date | "invalid";

const CLOCK = /^(\d{1,2})[:.h]?(\d{2})h?(\+*)$/i;

/**
 * `2329`, `23:29`, `2329H`, `929` and `9:29` all read as a clock time; a trailing `+` pushes it one
 * more day on top of the automatic rollover. Returns null for blank input and "invalid" for
 * anything else.
 */
export function parseClock(text: string): ClockTime | "invalid" | null {
  const value = text.trim().replace(/\s+/g, "");
  if (!value) {
    return null;
  }

  const match = CLOCK.exec(value);
  if (!match) {
    return "invalid";
  }

  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  if (hours > 23 || minutes > 59) {
    return "invalid";
  }

  return { minutes: hours * 60 + minutes, extraDays: match[3].length };
}

/**
 * Gives every typed time its date. The chain starts at midnight of `startDay`; each time lands on
 * the first moment at or after the previous one, so "2329" then "0323" is 23:29 and 03:23 the next
 * morning. Recorded times anchor the chain as they are. Blank entries are skipped.
 */
export function resolveChain(startDay: Date, entries: ChainEntry[]): Map<string, ResolvedTime> {
  const resolved = new Map<string, ResolvedTime>();
  let cursor = new Date(startDay.getFullYear(), startDay.getMonth(), startDay.getDate());

  for (const entry of entries) {
    if (entry.fixed) {
      resolved.set(entry.key, entry.fixed);
      cursor = entry.fixed;
      continue;
    }

    const clock = parseClock(entry.text);
    if (clock === null) {
      continue;
    }
    if (clock === "invalid") {
      resolved.set(entry.key, "invalid");
      continue;
    }

    const at = new Date(
      cursor.getFullYear(),
      cursor.getMonth(),
      cursor.getDate(),
      Math.floor(clock.minutes / 60),
      clock.minutes % 60,
    );
    if (at < cursor) {
      at.setDate(at.getDate() + 1);
    }
    at.setDate(at.getDate() + clock.extraDays);

    resolved.set(entry.key, at);
    cursor = at;
  }

  return resolved;
}

/** How a recorded time reads back in the report's own format: "2329". */
export function toClockText(value: string | Date | null | undefined): string {
  if (!value) {
    return "";
  }
  const at = new Date(value);
  return `${pad(at.getHours())}${pad(at.getMinutes())}`;
}

/** "Tue 23 Sep, 23:29" - shown under each field so an overnight rollover is visible. */
export function describeTime(at: Date): string {
  const day = at.toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" });
  return `${day}, ${pad(at.getHours())}:${pad(at.getMinutes())}`;
}

function pad(n: number): string {
  return n.toString().padStart(2, "0");
}
