#!/usr/bin/env node
/**
 * Build script: converts the city.json + barangay.json source files into
 * pretty-printed JSON in public/assets/ph-address/.
 *
 * The city source file is public/assets/ph-address/city-source.json
 * The barangay source file is public/assets/ph-address/barangay-source.json
 *
 * Run: node tools/convert-ph-address.mjs
 *
 * Region + Province are written directly as static JSON; only city + barangay
 * need this build step because they are too large to pretty-print by hand.
 */
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(
  __dirname,
  "../src/Client/Trackwise.Angular/projects/tms-portal/public/assets/ph-address",
);
const SRC = OUT; // source and output are the same dir

mkdirSync(OUT, { recursive: true });

function write(name, data) {
  const path = resolve(OUT, name);
  writeFileSync(path, JSON.stringify(data, null, 2) + "\n", "utf8");
  console.log(`wrote ${path}  (${data.length} rows)`);
}

function read(name) {
  return JSON.parse(readFileSync(resolve(SRC, name), "utf8"));
}

// Convert city-source.json → city.json
// Source shape: { city_code, city_name, province_code, psgc_code, region_desc }
// Target shape (API):  { city_code, city_name, province_code, psgc_code, region_code }
try {
  const cities = read("city-source.json");
  const converted = cities.map((c) => ({
    city_code: c.city_code,
    city_name: c.city_name,
    province_code: c.province_code,
    psgc_code: c.psgc_code,
    region_code: c.region_desc, // <-- rename
  }));
  write("city.json", converted);
} catch (e) {
  console.warn("city-source.json not found, skipping city.json");
}

// Convert barangay-source.json → barangay.json
// Source shape: { brgy_code, brgy_name, city_code, province_code, region_code }
try {
  const barangays = read("barangay-source.json");
  write("barangay.json", barangays);
} catch (e) {
  console.warn("barangay-source.json not found, skipping barangay.json");
}
