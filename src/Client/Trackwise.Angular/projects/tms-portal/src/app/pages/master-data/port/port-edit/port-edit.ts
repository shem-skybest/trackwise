import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deletePort,
  getPortById,
  updatePort,
  type TerminalDto,
  type UpdatePortCommand,
} from "@trackwise/shared/api";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import { PortForm, type PortFormValue } from "../components/port-form/port-form";

/**
 * The API's answer when the dependency probe stops a retirement. It carries the counts the steward
 * needs, so the message is shown verbatim rather than replaced with something generic.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-port-edit",
  templateUrl: "./port-edit.html",
  imports: [PageHeader, PortForm, MasterDataHistory, Spinner, UiButton],
})
export class PortEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly port = signal<TerminalDto | null>(null);
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
      this.port.set(await this.api.invoke(getPortById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: PortFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdatePortCommand = {
      name: formValue.name,
      countryCode: formValue.countryCode,
      type: formValue.type,
      address: formValue.address ?? undefined,
      notes: formValue.notes,
      branchId: formValue.branchId,
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updatePort, { id: this.id, body: command });

      this.toastService.showSuccess("Port updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        this.retirementMessage(error) ??
          "Failed to update port. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("port", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deletePort, { id: this.id });

      this.toastService.showSuccess("Port deleted. It can be restored from the ports list.");

      await this.router.navigateByUrl("/master-data/ports");
    } catch (error) {
      this.toastService.showError(this.retirementMessage(error) ?? "Failed to delete port.");
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * The dependency probe's own message, which names what is still using the port and how much of
   * it. Overriding it is a deliberate, recorded act and stays API-only for now, so there is no
   * one-click "delete anyway" here to make it look casual.
   */
  private retirementMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }
}
