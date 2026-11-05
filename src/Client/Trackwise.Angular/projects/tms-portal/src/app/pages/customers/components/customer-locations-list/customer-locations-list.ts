import { Component, effect, inject, input, untracked } from "@angular/core";
import { RouterLink } from "@angular/router";
import { getLocations, type LocationDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";
import {
  Badge,
  Stack,
  UiButton,
  UiDataTable,
  UiFormField,
  UiSortHeader,
  UiTextField,
} from "@trackwise/shared/ui";
import { formatTimeOfDay } from "@/shared/utils";

const CustomerLocationsStore = createListStore<LocationDto>(getLocations, {
  defaultSortField: "Name",
  defaultPageSize: 25,
});

/** The customer's own delivery locations (outlets, stores, schools), paged server-side. */
@Component({
  selector: "app-customer-locations-list",
  templateUrl: "./customer-locations-list.html",
  providers: [CustomerLocationsStore],
  imports: [
    Badge,
    RouterLink,
    Stack,
    UiButton,
    UiDataTable,
    UiFormField,
    UiSortHeader,
    UiTextField,
  ],
})
export class CustomerLocationsList {
  protected readonly store = inject(CustomerLocationsStore);
  protected readonly formatTimeOfDay = formatTimeOfDay;

  public readonly customerId = input.required<string>();

  constructor() {
    // Filter only: the table's first lazy-load fetches, so reloading here would double it.
    effect(() => {
      const customerId = this.customerId();
      untracked(() => this.store.setFilters({ CustomerId: customerId }, { reload: false }));
    });
  }

  protected place(location: LocationDto): string {
    const address = location.address;
    if (!address) {
      return "";
    }
    return [address.streetAddress, address.cityOrMunicipality].filter(Boolean).join(", ");
  }
}
