import { Component, effect, inject, input, signal } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  createLocation,
  getCustomerById,
  type CreateLocationCommand,
  type CustomerDto,
} from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { LocationForm, type LocationFormValue } from "../components/location-form/location-form";

@Component({
  selector: "app-location-add",
  templateUrl: "./location-add.html",
  imports: [PageHeader, LocationForm],
})
export class LocationAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  /** `?customerId=` from a customer's page: the new location starts as that customer's outlet. */
  protected readonly customerId = input<string>();
  protected readonly initialCustomer = signal<CustomerDto | null>(null);

  constructor() {
    effect(() => {
      const id = this.customerId();
      if (id) {
        void this.api
          .invoke(getCustomerById, { id })
          .then((customer) => this.initialCustomer.set(customer))
          .catch(() => undefined);
      }
    });
  }

  protected async create(formValue: LocationFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateLocationCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      type: formValue.type,
      address: formValue.address ?? undefined,
      parentLocationId: formValue.parentLocationId,
      latitude: formValue.latitude,
      longitude: formValue.longitude,
      notes: formValue.notes,
      branchId: formValue.branchId,
      customerId: formValue.customerId,
      defaultCutoffTime: formValue.defaultCutoffTime,
      zoneRadiusMeters: formValue.zoneRadiusMeters,
    };

    try {
      const location = await this.api.invoke(createLocation, { body: command });

      this.toastService.showSuccess(`Location ${location.code} created`);

      await this.router.navigateByUrl(
        this.customerId() ? `/customers/${this.customerId()}` : "/master-data/locations",
      );
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // location rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create location. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
