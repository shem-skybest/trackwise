import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deleteHelper,
  getHelperById,
  updateHelper,
  type HelperDto,
  type UpdateHelperCommand,
} from "@trackwise/shared/api";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import { HelperForm, type HelperFormValue } from "../components/helper-form/helper-form";

/**
 * The API's answer when the dependency probe stops a retirement. It carries the counts the steward
 * needs, so the message is shown verbatim rather than replaced with something generic.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-helper-edit",
  templateUrl: "./helper-edit.html",
  imports: [PageHeader, HelperForm, MasterDataHistory, Spinner, UiButton],
})
export class HelperEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly helper = signal<HelperDto | null>(null);
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
      this.helper.set(await this.api.invoke(getHelperById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: HelperFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdateHelperCommand = {
      name: formValue.name,
      phoneNumber: formValue.phoneNumber ?? undefined,
      address: formValue.address ?? undefined,
      notes: formValue.notes ?? undefined,
      branchId: formValue.branchId ?? undefined,
      employmentType: formValue.employmentType ?? undefined,
      homeUnit: formValue.homeUnit ?? undefined,
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updateHelper, { id: this.id, body: command });

      this.toastService.showSuccess("Helper updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        this.retirementMessage(error) ??
          "Failed to update helper. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("helper", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deleteHelper, { id: this.id });

      this.toastService.showSuccess("Helper deleted. It can be restored from the helpers list.");

      await this.router.navigateByUrl("/master-data/helpers");
    } catch (error) {
      this.toastService.showError(this.retirementMessage(error) ?? "Failed to delete helper.");
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * The dependency probe's own message, which names what is still using the helper and how much of
   * it. Overriding it is a deliberate, recorded act and stays API-only for now, so there is no
   * one-click "delete anyway" here to make it look casual.
   */
  private retirementMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }
}
