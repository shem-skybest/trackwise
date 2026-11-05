import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createTrailer, type CreateTrailerCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { TrailerForm, type TrailerFormValue } from "../components/trailer-form/trailer-form";

@Component({
  selector: "app-trailer-add",
  templateUrl: "./trailer-add.html",
  imports: [PageHeader, TrailerForm],
})
export class TrailerAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: TrailerFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateTrailerCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      type: formValue.type,
      vin: formValue.vin ?? undefined,
      licensePlate: formValue.licensePlate ?? undefined,
      licensePlateState: formValue.licensePlateState ?? undefined,
      make: formValue.make ?? undefined,
      model: formValue.model ?? undefined,
      year: formValue.year ?? undefined,
      axleCount: formValue.axleCount ?? undefined,
      maxLoadCapacityKg: formValue.maxLoadCapacityKg ?? undefined,
      notes: formValue.notes ?? undefined,
      branchId: formValue.branchId ?? undefined,
    };

    try {
      const trailer = await this.api.invoke(createTrailer, { body: command });

      this.toastService.showSuccess(`Trailer ${trailer.code} created`);

      await this.router.navigateByUrl("/master-data/trailers");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // trailer rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create trailer. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
