import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deleteCommodity,
  getCommodityById,
  updateCommodity,
  type CommodityDto,
  type UpdateCommodityCommand,
} from "@trackwise/shared/api";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import {
  CommodityForm,
  type CommodityFormValue,
} from "../components/commodity-form/commodity-form";

/**
 * The API's answer when the dependency probe stops a retirement. It carries the counts the steward
 * needs, so the message is shown verbatim rather than replaced with something generic.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-commodity-edit",
  templateUrl: "./commodity-edit.html",
  imports: [PageHeader, CommodityForm, MasterDataHistory, Spinner, UiButton],
})
export class CommodityEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly commodity = signal<CommodityDto | null>(null);
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
      this.commodity.set(await this.api.invoke(getCommodityById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: CommodityFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdateCommodityCommand = {
      name: formValue.name,
      hsCode: formValue.hsCode,
      description: formValue.description,
      isHazardous: formValue.isHazardous,
      unNumber: formValue.unNumber,
      requiresTemperatureControl: formValue.requiresTemperatureControl,
      notes: formValue.notes,
      branchId: formValue.branchId,
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updateCommodity, { id: this.id, body: command });

      this.toastService.showSuccess("Commodity updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        this.retirementMessage(error) ??
          "Failed to update commodity. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("commodity", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deleteCommodity, { id: this.id });

      this.toastService.showSuccess(
        "Commodity deleted. It can be restored from the commodities list.",
      );

      await this.router.navigateByUrl("/master-data/commodities");
    } catch (error) {
      this.toastService.showError(this.retirementMessage(error) ?? "Failed to delete commodity.");
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * The dependency probe's own message, which names what is still using the commodity and how much
   * of it. Overriding it is a deliberate, recorded act and stays API-only for now, so there is no
   * one-click "delete anyway" here to make it look casual.
   */
  private retirementMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }
}
