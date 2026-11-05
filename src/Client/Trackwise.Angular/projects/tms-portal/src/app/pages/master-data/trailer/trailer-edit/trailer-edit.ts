import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deleteTrailer,
  getTrailerById,
  updateTrailer,
  type TrailerDto,
  type UpdateTrailerCommand,
} from "@trackwise/shared/api";
import { Card, Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader, VehiclePapers } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import { TrailerForm, type TrailerFormValue } from "../components/trailer-form/trailer-form";

/**
 * The API's answer when the dependency probe stops a retirement. It carries the counts the steward
 * needs, so the message is shown verbatim rather than replaced with something generic.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-trailer-edit",
  templateUrl: "./trailer-edit.html",
  imports: [Card, PageHeader, TrailerForm, MasterDataHistory, Spinner, UiButton, VehiclePapers],
})
export class TrailerEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly trailer = signal<TrailerDto | null>(null);
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
      this.trailer.set(await this.api.invoke(getTrailerById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: TrailerFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdateTrailerCommand = {
      name: formValue.name,
      type: formValue.type,
      vin: formValue.vin ?? undefined,
      licensePlate: formValue.licensePlate ?? undefined,
      licensePlateState: formValue.licensePlateState ?? undefined,
      make: formValue.make ?? undefined,
      model: formValue.model ?? undefined,
      year: formValue.year ?? undefined,
      axleCount: formValue.axleCount ?? undefined,
      maxLoadCapacityKg: formValue.maxLoadCapacityKg ?? undefined,
      notes: formValue.notes ?? undefined,
      branchId: formValue.branchId ?? undefined,
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updateTrailer, { id: this.id, body: command });

      this.toastService.showSuccess("Trailer updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        this.retirementMessage(error) ??
          "Failed to update trailer. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("trailer", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deleteTrailer, { id: this.id });

      this.toastService.showSuccess("Trailer deleted. It can be restored from the trailers list.");

      await this.router.navigateByUrl("/master-data/trailers");
    } catch (error) {
      this.toastService.showError(this.retirementMessage(error) ?? "Failed to delete trailer.");
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * The dependency probe's own message, which names what is still using the trailer and how much
   * of it. Overriding it is a deliberate, recorded act and stays API-only for now, so there is no
   * one-click "delete anyway" here to make it look casual.
   */
  private retirementMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }
}
