import { parseClock, resolveChain, toClockText } from "./report-times";

describe("parseClock", () => {
  it.each([
    ["2329", 23 * 60 + 29],
    ["23:29", 23 * 60 + 29],
    ["2329H", 23 * 60 + 29],
    ["2329h", 23 * 60 + 29],
    ["929", 9 * 60 + 29],
    ["9:29", 9 * 60 + 29],
    ["0000", 0],
  ])("reads %s", (text, minutes) => {
    expect(parseClock(text)).toEqual({ minutes, extraDays: 0 });
  });

  it("counts trailing + as extra days", () => {
    expect(parseClock("0323++")).toEqual({ minutes: 3 * 60 + 23, extraDays: 2 });
  });

  it("returns null for blank input", () => {
    expect(parseClock("  ")).toBeNull();
  });

  it.each(["2460", "2400", "abc", "12345", "23:5"])("rejects %s", (text) => {
    expect(parseClock(text)).toBe("invalid");
  });
});

describe("resolveChain", () => {
  const preLoad = new Date(2026, 8, 23);

  it("rolls a time earlier than the one before it to the next day (ABC1267)", () => {
    const resolved = resolveChain(preLoad, [
      { key: "garageOut", text: "2147" },
      { key: "whArrival", text: "2329H" },
      { key: "loadStart", text: "0323H" },
      { key: "loadDone", text: "0345H" },
      { key: "whOut", text: "0410H" },
    ]);

    expect(resolved.get("garageOut")).toEqual(new Date(2026, 8, 23, 21, 47));
    expect(resolved.get("whArrival")).toEqual(new Date(2026, 8, 23, 23, 29));
    expect(resolved.get("loadStart")).toEqual(new Date(2026, 8, 24, 3, 23));
    expect(resolved.get("whOut")).toEqual(new Date(2026, 8, 24, 4, 10));
  });

  it("keeps the same day when times move forward", () => {
    const resolved = resolveChain(preLoad, [
      { key: "a", text: "0800" },
      { key: "b", text: "0800" },
      { key: "c", text: "1330" },
    ]);

    expect(resolved.get("b")).toEqual(new Date(2026, 8, 23, 8, 0));
    expect(resolved.get("c")).toEqual(new Date(2026, 8, 23, 13, 30));
  });

  it("anchors on recorded times and skips blanks", () => {
    const recorded = new Date(2026, 8, 24, 5, 0);
    const resolved = resolveChain(preLoad, [
      { key: "a", text: "", fixed: recorded },
      { key: "b", text: "" },
      { key: "c", text: "0430" },
    ]);

    expect(resolved.get("a")).toBe(recorded);
    expect(resolved.has("b")).toBe(false);
    expect(resolved.get("c")).toEqual(new Date(2026, 8, 25, 4, 30));
  });

  it("marks unreadable entries without moving the chain", () => {
    const resolved = resolveChain(preLoad, [
      { key: "a", text: "0900" },
      { key: "b", text: "9x" },
      { key: "c", text: "1000" },
    ]);

    expect(resolved.get("b")).toBe("invalid");
    expect(resolved.get("c")).toEqual(new Date(2026, 8, 23, 10, 0));
  });
});

describe("toClockText", () => {
  it("formats a recorded time the way it was typed", () => {
    expect(toClockText(new Date(2026, 8, 24, 3, 5))).toBe("0305");
  });
});
