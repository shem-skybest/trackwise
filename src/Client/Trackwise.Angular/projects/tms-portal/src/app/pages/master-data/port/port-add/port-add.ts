import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createPort, type CreatePortCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { PortForm, type PortFormValue } from "../components/port-form/port-form";

@Component({
  selector: "app-port-add",
  templateUrl: "./port-add.html",
  imports: [PageHeader, PortForm],
})
export class PortAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: PortFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreatePortCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      countryCode: formValue.countryCode,
      type: formValue.type,
      address: formValue.address ?? undefined,
      notes: formValue.notes,
      branchId: formValue.branchId,
    };

    try {
      const port = await this.api.invoke(createPort, { body: command });

      this.toastService.showSuccess(`Port ${port.code} created`);

      await this.router.navigateByUrl("/master-data/ports");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // port rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create port. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
