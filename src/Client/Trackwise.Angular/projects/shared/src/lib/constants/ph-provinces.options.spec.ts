import {
  canonicalPhProvince,
  PH_PROVINCES_OPTIONS,
  phProvinceAndRegion,
  phRegionOfProvince,
} from "./ph-provinces.options";

describe("PH provinces", () => {
  it("lists all 82 provinces plus Metro Manila", () => {
    expect(PH_PROVINCES_OPTIONS.length).toBe(83);
  });

  it("finds the region of a province in any casing", () => {
    expect(phRegionOfProvince("Laguna")).toBe("CALABARZON");
    expect(phRegionOfProvince("zamboanga del sur")).toBe("Zamboanga Peninsula");
    expect(phRegionOfProvince("Metro Manila")).toBe("NCR");
  });

  it("folds PSGC's Metro Manila districts into Metro Manila", () => {
    expect(canonicalPhProvince("Ncr, City Of Manila, First District")).toBe("Metro Manila");
    expect(canonicalPhProvince("Ncr, Fourth District")).toBe("Metro Manila");
  });

  it("reads PSGC's old and qualified province names", () => {
    expect(canonicalPhProvince("Samar (Western Samar)")).toBe("Samar");
    expect(canonicalPhProvince("Compostela Valley")).toBe("Davao de Oro");
    expect(canonicalPhProvince("Davao Del Norte")).toBe("Davao del Norte");
  });

  it("returns null for anything that isn't a province", () => {
    expect(canonicalPhProvince("Texas")).toBeNull();
    expect(phRegionOfProvince("")).toBeNull();
    expect(phRegionOfProvince(null)).toBeNull();
  });

  it("keeps the source's own names when the province isn't recognised", () => {
    expect(phProvinceAndRegion("City Of Isabela", "Region IX (Zamboanga Peninzula)")).toEqual({
      province: "City Of Isabela",
      region: "Region IX (Zamboanga Peninzula)",
    });
  });

  it("replaces a Mapbox province echoed into the region", () => {
    expect(phProvinceAndRegion("Metro Manila", "Metro Manila")).toEqual({
      province: "Metro Manila",
      region: "NCR",
    });
  });
});
