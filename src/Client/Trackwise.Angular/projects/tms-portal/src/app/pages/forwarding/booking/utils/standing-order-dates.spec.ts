import { MAX_REPEAT_DAYS, standingOrderDates } from "./standing-order-dates";

const MONDAY = 1;
const THURSDAY = 4;

function iso(dates: Date[]): string[] {
  return dates.map(
    (d) =>
      `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`,
  );
}

describe("standingOrderDates", () => {
  it("returns every Monday in the range, both ends included", () => {
    const dates = standingOrderDates(new Date(2026, 9, 5), new Date(2026, 9, 26), [MONDAY]);

    expect(iso(dates)).toEqual(["2026-10-05", "2026-10-12", "2026-10-19", "2026-10-26"]);
  });

  it("interleaves several weekdays in date order", () => {
    const dates = standingOrderDates(new Date(2026, 9, 5), new Date(2026, 9, 15), [
      THURSDAY,
      MONDAY,
    ]);

    expect(iso(dates)).toEqual(["2026-10-05", "2026-10-08", "2026-10-12", "2026-10-15"]);
  });

  it("is empty when the range ends before it starts or no weekday is picked", () => {
    expect(standingOrderDates(new Date(2026, 9, 12), new Date(2026, 9, 5), [MONDAY])).toEqual([]);
    expect(standingOrderDates(new Date(2026, 9, 5), new Date(2026, 9, 26), [])).toEqual([]);
  });

  it("stops one past the limit so the caller can report a run that is too long", () => {
    const everyDay = [0, 1, 2, 3, 4, 5, 6];

    const dates = standingOrderDates(new Date(2026, 0, 1), new Date(2027, 0, 1), everyDay);

    expect(dates).toHaveLength(MAX_REPEAT_DAYS + 1);
  });
});
