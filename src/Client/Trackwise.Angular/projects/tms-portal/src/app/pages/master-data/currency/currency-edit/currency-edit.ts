import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deleteCurrency,
  getCurrencyById,
  updateCurrency,
  type CurrencyDto,
  type UpdateCurrencyCommand,
} from "@trackwise/shared/api";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import { CurrencyForm, type CurrencyFormValue } from "../components/currency-form/currency-form";

@Component({
  selector: "app-currency-edit",
  templateUrl: "./currency-edit.html",
  imports: [PageHeader, CurrencyForm, MasterDataHistory, Spinner, UiButton],
})
export class CurrencyEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly currency = signal<CurrencyDto | null>(null);
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
      this.currency.set(await this.api.invoke(getCurrencyById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: CurrencyFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdateCurrencyCommand = {
      name: formValue.name,
      numericCode: formValue.numericCode,
      symbol: formValue.symbol,
      minorUnits: formValue.minorUnits,
      notes: formValue.notes,
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updateCurrency, { id: this.id, body: command });

      this.toastService.showSuccess("Currency updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        (error as { error?: { message?: string } })?.error?.message ??
          "Failed to update currency. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("currency", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deleteCurrency, { id: this.id });

      this.toastService.showSuccess(
        "Currency deleted. It can be restored from the currencies list.",
      );

      await this.router.navigateByUrl("/master-data/currencies");
    } catch (error) {
      this.toastService.showError(
        (error as { error?: { message?: string } })?.error?.message ?? "Failed to delete currency.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }
}
