import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createQuotation, type CreateQuotationCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { QuotationForm } from "../components/quotation-form/quotation-form";

@Component({
  selector: "app-quotation-add",
  templateUrl: "./quotation-add.html",
  imports: [PageHeader, QuotationForm],
})
export class QuotationAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(command: CreateQuotationCommand): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    try {
      // console.log(command);
      const id = await this.api.invoke(createQuotation, { body: command });

      this.toastService.showSuccess("A new quotation has been created successfully");

      await this.router.navigateByUrl(`/quotations/${id}`);
    } catch {
      this.toastService.showError(
        "Failed to create quotation. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
