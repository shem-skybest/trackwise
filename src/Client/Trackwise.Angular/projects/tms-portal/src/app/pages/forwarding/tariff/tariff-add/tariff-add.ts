import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createTariff, type CreateTariffCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader, TariffForm, type TariffFormValue } from "@/shared/components";

@Component({
  selector: "app-tariff-add",
  templateUrl: "./tariff-add.html",
  imports: [PageHeader, TariffForm],
})
export class TariffAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: TariffFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateTariffCommand = {
      name: formValue.name.trim(),
      mode: formValue.mode,
      serviceLevel: formValue.serviceLevel,
      originLane: formValue.originLane ?? undefined,
      destinationLane: formValue.destinationLane ?? undefined,
      containerTypeId: formValue.containerTypeId ?? undefined,
      currency: formValue.currency.trim().toUpperCase(),
      effectiveFrom: formValue.effectiveFrom,
      carrierId: formValue.carrierId ?? undefined,
      contractId: formValue.contractId ?? undefined,
    };

    try {
      await this.api.invoke(createTariff, {
        body: command,
      });

      this.toastService.showSuccess("A new tariff has been created successfully");

      await this.router.navigateByUrl("/tariffs");
    } catch {
      this.toastService.showError(
        "Failed to create tariff. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
