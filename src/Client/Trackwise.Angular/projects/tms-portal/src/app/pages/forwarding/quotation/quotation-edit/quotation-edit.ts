import { Component, inject, input, resource, signal } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  getQuotationById,
  updateQuotation,
  type CreateQuotationCommand,
} from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { QuotationForm } from "../components/quotation-form/quotation-form";

@Component({
  selector: "app-quotation-edit",
  templateUrl: "./quotation-edit.html",
  imports: [PageHeader, QuotationForm],
})
export class QuotationEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  // bound from the :id route param
  public readonly id = input.required<string>();

  protected readonly isLoading = signal(false);

  protected readonly quotation = resource({
    params: () => this.id(),
    loader: ({ params: id }) => this.api.invoke(getQuotationById, { id }),
  });

  protected async update(command: CreateQuotationCommand): Promise<void> {
    if (this.isLoading()) return;

    this.isLoading.set(true);

    try {
      await this.api.invoke(updateQuotation, { id: this.id(), body: command });
      this.toastService.showSuccess("Quotation has been updated successfully");
      await this.router.navigateByUrl(`/quotations/${this.id()}`);
    } catch {
      this.toastService.showError(
        "Failed to update quotation. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
