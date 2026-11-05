import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createHelper, type CreateHelperCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { HelperForm, type HelperFormValue } from "../components/helper-form/helper-form";

@Component({
  selector: "app-helper-add",
  templateUrl: "./helper-add.html",
  imports: [PageHeader, HelperForm],
})
export class HelperAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: HelperFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateHelperCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      phoneNumber: formValue.phoneNumber ?? undefined,
      address: formValue.address ?? undefined,
      notes: formValue.notes ?? undefined,
      branchId: formValue.branchId ?? undefined,
      employmentType: formValue.employmentType ?? undefined,
      homeUnit: formValue.homeUnit ?? undefined,
    };

    try {
      const helper = await this.api.invoke(createHelper, { body: command });

      this.toastService.showSuccess(`Helper ${helper.code} created`);

      await this.router.navigateByUrl("/master-data/helpers");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // helper rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create helper. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
