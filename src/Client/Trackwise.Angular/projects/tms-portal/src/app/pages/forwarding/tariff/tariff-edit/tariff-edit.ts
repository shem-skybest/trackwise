import { Component, computed, inject, input, signal, type OnInit } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  getTariffById,
  updateTariff,
  type TariffDto,
  type UpdateTariffCommand,
} from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader, TariffForm, type TariffFormValue } from "@/shared/components";

@Component({
  selector: "app-tariff-edit",
  templateUrl: "./tariff-edit.html",
  imports: [PageHeader, TariffForm],
})
export class TariffEditComponent implements OnInit {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  public readonly id = input.required<string>();
  protected readonly tariff = signal<TariffDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly initialFormValue = computed(() => {
    const tariff = this.tariff();

    return tariff
      ? {
          name: tariff.name ?? "",
          mode: tariff.mode,
          serviceLevel: tariff.serviceLevel,
          originLane: tariff.originLane ?? null,
          destinationLane: tariff.destinationLane ?? null,
          containerTypeId: tariff.containerTypeId ?? null,
          currency: tariff.currency ?? "USD",
          effectiveFrom: tariff.effectiveFrom ?? "",
          carrierId: tariff.carrierId ?? null,
          contractId: tariff.contractId ?? null,
          customerId: tariff.customerId ?? null,
          customerName: tariff.customerName ?? null,
        }
      : null;
  });

  ngOnInit(): void {
    void this.loadTariff();
  }

  protected async update(formValue: TariffFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: UpdateTariffCommand = {
      mode: formValue.mode,
      serviceLevel: formValue.serviceLevel,
      name: formValue.name.trim(),
      currency: formValue.currency.trim().toUpperCase(),
      effectiveFrom: formValue.effectiveFrom,
      originLane: formValue.originLane ?? undefined,
      destinationLane: formValue.destinationLane ?? undefined,
      containerTypeId: formValue.containerTypeId ?? undefined,
      carrierId: formValue.carrierId ?? undefined,
      contractId: formValue.contractId ?? undefined,
    };

    try {
      await this.api.invoke(updateTariff, { id: this.id(), body: command });
      this.toastService.showSuccess("Tariff updated successfully");
      await this.router.navigateByUrl(`/tariffs/${this.id()}`);
    } catch {
      this.toastService.showError("Failed to update tariff");
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadTariff(): Promise<void> {
    try {
      this.tariff.set(await this.api.invoke(getTariffById, { id: this.id() }));
    } catch {
      this.toastService.showError("Failed to load tariff");
    } finally {
      this.isLoading.set(false);
    }
  }
}
