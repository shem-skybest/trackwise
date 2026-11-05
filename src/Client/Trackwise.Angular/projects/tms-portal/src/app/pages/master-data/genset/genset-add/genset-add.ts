import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createGenset, type CreateGensetCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { GensetForm, type GensetFormValue } from "../components/genset-form/genset-form";

@Component({
  selector: "app-genset-add",
  templateUrl: "./genset-add.html",
  imports: [PageHeader, GensetForm],
})
export class GensetAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: GensetFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateGensetCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      status: formValue.status,
      serialNumber: formValue.serialNumber ?? undefined,
      make: formValue.make ?? undefined,
      model: formValue.model ?? undefined,
      notes: formValue.notes ?? undefined,
      branchId: formValue.branchId ?? undefined,
    };

    try {
      const genset = await this.api.invoke(createGenset, { body: command });

      this.toastService.showSuccess(`Genset ${genset.code} created`);

      await this.router.navigateByUrl("/master-data/gensets");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // genset rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create genset. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
