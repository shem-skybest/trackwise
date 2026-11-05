import { Pipe, type PipeTransform } from "@angular/core";
import type { Address } from "../api/generated/models";

/**
 * Formats an Address object into a readable string.
 * Usage: {{ address | address }} or {{ address | address:'short' }}
 */
@Pipe({ name: "address" })
export class AddressPipe implements PipeTransform {
  /**
   * Transforms an Address object into a formatted string. E.g. "123 Main St, Springfield, IL, 62701"
   * @param address Address object to format
   * @param format "full" for full address, "short" for city and province only
   * @returns Formatted address string or "-" if address is null/invalid
   */
  transform(address: Address | null | undefined, format: "full" | "short" = "full"): string {
    if (this.isNullAddress(address)) return "-";

    if (format === "short") {
      const parts = [address.cityOrMunicipality, address.province].filter(Boolean);
      return parts.length > 0 ? parts.join(", ") : "-";
    }

    const parts = [
      address.streetAddress,
      address.cityOrMunicipality,
      address.province,
      address.zipcode,
    ].filter(Boolean);
    return parts.length > 0 ? parts.join(", ") : "-";
  }

  /**
   * Checks if the address is null or contains invalid values
   * @param value Address object
   * @returns true if the address is null or contains invalid values, false otherwise
   */
  private isNullAddress(value?: Address | null): value is null | undefined {
    return (
      value == null ||
      value.streetAddress == null ||
      value.streetAddress === "NULL" ||
      value.cityOrMunicipality == null ||
      value.cityOrMunicipality === "NULL" ||
      value.province == null ||
      value.province === "NULL" ||
      value.zipcode == null ||
      value.zipcode === "NULL" ||
      value.country == null ||
      value.country === "NULL"
    );
  }
}
