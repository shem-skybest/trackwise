import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deleteContainerType,
  getContainerTypeById,
  updateContainerType,
  type ContainerTypeCategory,
  type ContainerTypeDto,
  type UpdateContainerTypeCommand,
} from "@trackwise/shared/api";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import {
  ContainerTypeForm,
  type ContainerTypeFormValue,
} from "../components/container-type-form/container-type-form";

/**
 * The API's answer when the dependency probe stops a retirement. It carries the counts the steward
 * needs, so the message is shown verbatim rather than replaced with something generic.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-container-type-edit",
  templateUrl: "./container-type-edit.html",
  imports: [PageHeader, ContainerTypeForm, MasterDataHistory, Spinner, UiButton],
})
export class ContainerTypeEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly containerType = signal<ContainerTypeDto | null>(null);
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
      this.containerType.set(await this.api.invoke(getContainerTypeById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: ContainerTypeFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdateContainerTypeCommand = {
      name: formValue.name,
      isoSizeTypeCode: formValue.isoSizeTypeCode,
      category: formValue.category as ContainerTypeCategory,
      lengthFeet: formValue.lengthFeet,
      isHighCube: formValue.isHighCube,
      teuFactor: formValue.teuFactor,
      notes: formValue.notes,
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updateContainerType, { id: this.id, body: command });

      this.toastService.showSuccess("Container type updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        this.retirementMessage(error) ??
          "Failed to update container type. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("container type", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deleteContainerType, { id: this.id });

      this.toastService.showSuccess(
        "Container type deleted. It can be restored from the container types list.",
      );

      await this.router.navigateByUrl("/master-data/container-types");
    } catch (error) {
      this.toastService.showError(
        this.retirementMessage(error) ?? "Failed to delete container type.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * The dependency probe's own message, which names what is still using the type and how much of
   * it - containers, bookings and tariffs. Overriding it is a deliberate, recorded act and stays
   * API-only for now, so there is no one-click "delete anyway" here to make it look casual.
   */
  private retirementMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }
}
