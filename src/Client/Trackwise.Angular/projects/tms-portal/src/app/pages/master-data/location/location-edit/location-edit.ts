import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deleteLocation,
  getLocationById,
  updateLocation,
  type LocationDto,
  type UpdateLocationCommand,
} from "@trackwise/shared/api";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import { LocationForm, type LocationFormValue } from "../components/location-form/location-form";

/**
 * The API's answer when the dependency probe stops a retirement. For a zone it names the delivery
 * points still inside it, so the message is shown verbatim rather than replaced with something
 * generic.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-location-edit",
  templateUrl: "./location-edit.html",
  imports: [PageHeader, LocationForm, MasterDataHistory, Spinner, UiButton],
})
export class LocationEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly location = signal<LocationDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);

  /** Bumped after a save so the history panel re-reads instead of showing a stale trail. */
  protected readonly historyVersion = signal(0);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    if (!this.id) {
      return;
    }

    this.isLoading.set(true);
    try {
      this.location.set(await this.api.invoke(getLocationById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: LocationFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdateLocationCommand = {
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
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updateLocation, { id: this.id, body: command });

      this.toastService.showSuccess("Location updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        this.retirementMessage(error) ??
          "Failed to update location. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("location", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deleteLocation, { id: this.id });

      this.toastService.showSuccess(
        "Location deleted. It can be restored from the locations list.",
      );

      await this.router.navigateByUrl("/master-data/locations");
    } catch (error) {
      this.toastService.showError(this.retirementMessage(error) ?? "Failed to delete location.");
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * The dependency probe's own message, which names what is still using the location and how much
   * of it. Overriding it is a deliberate, recorded act and stays API-only for now, so there is no
   * one-click "delete anyway" here to make it look casual.
   */
  private retirementMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }
}
