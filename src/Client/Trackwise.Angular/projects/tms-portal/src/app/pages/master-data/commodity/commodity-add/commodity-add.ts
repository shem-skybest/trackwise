import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createCommodity, type CreateCommodityCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import {
  CommodityForm,
  type CommodityFormValue,
} from "../components/commodity-form/commodity-form";

@Component({
  selector: "app-commodity-add",
  templateUrl: "./commodity-add.html",
  imports: [PageHeader, CommodityForm],
})
export class CommodityAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: CommodityFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateCommodityCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      hsCode: formValue.hsCode,
      description: formValue.description,
      isHazardous: formValue.isHazardous,
      unNumber: formValue.unNumber,
      requiresTemperatureControl: formValue.requiresTemperatureControl,
      notes: formValue.notes,
      branchId: formValue.branchId,
    };

    try {
      const commodity = await this.api.invoke(createCommodity, { body: command });

      this.toastService.showSuccess(`Commodity ${commodity.code} created`);

      await this.router.navigateByUrl("/master-data/commodities");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // commodity rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create commodity. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
