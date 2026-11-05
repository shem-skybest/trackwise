import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createVessel, type CreateVesselCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { VesselForm, type VesselFormValue } from "../components/vessel-form/vessel-form";

@Component({
  selector: "app-vessel-add",
  templateUrl: "./vessel-add.html",
  imports: [PageHeader, VesselForm],
})
export class VesselAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: VesselFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateVesselCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      imoNumber: formValue.imoNumber,
      callSign: formValue.callSign,
      flagCountryCode: formValue.flagCountryCode,
      type: formValue.type,
      teuCapacity: formValue.teuCapacity,
      notes: formValue.notes,
      branchId: formValue.branchId,
    };

    try {
      const vessel = await this.api.invoke(createVessel, { body: command });

      this.toastService.showSuccess(`Vessel ${vessel.code} created`);

      await this.router.navigateByUrl("/master-data/vessels");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // vessel rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create vessel. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
