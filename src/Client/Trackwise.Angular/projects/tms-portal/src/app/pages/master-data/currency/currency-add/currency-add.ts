import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createCurrency, type CreateCurrencyCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { CurrencyForm, type CurrencyFormValue } from "../components/currency-form/currency-form";

@Component({
  selector: "app-currency-add",
  templateUrl: "./currency-add.html",
  imports: [PageHeader, CurrencyForm],
})
export class CurrencyAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: CurrencyFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateCurrencyCommand = {
      code: formValue.code,
      name: formValue.name,
      numericCode: formValue.numericCode,
      symbol: formValue.symbol,
      minorUnits: formValue.minorUnits,
      notes: formValue.notes,
    };

    try {
      const currency = await this.api.invoke(createCurrency, { body: command });

      this.toastService.showSuccess(`Currency ${currency.code} created`);

      await this.router.navigateByUrl("/master-data/currencies");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // currency rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create currency. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
