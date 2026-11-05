import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  createContainerType,
  type ContainerTypeCategory,
  type CreateContainerTypeCommand,
} from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import {
  ContainerTypeForm,
  type ContainerTypeFormValue,
} from "../components/container-type-form/container-type-form";

@Component({
  selector: "app-container-type-add",
  templateUrl: "./container-type-add.html",
  imports: [PageHeader, ContainerTypeForm],
})
export class ContainerTypeAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: ContainerTypeFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateContainerTypeCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      isoSizeTypeCode: formValue.isoSizeTypeCode,
      category: formValue.category as ContainerTypeCategory,
      lengthFeet: formValue.lengthFeet,
      isHighCube: formValue.isHighCube,
      teuFactor: formValue.teuFactor,
      notes: formValue.notes,
    };

    try {
      const containerType = await this.api.invoke(createContainerType, { body: command });

      this.toastService.showSuccess(`Container type ${containerType.code} created`);

      await this.router.navigateByUrl("/master-data/container-types");
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // type rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create container type. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
