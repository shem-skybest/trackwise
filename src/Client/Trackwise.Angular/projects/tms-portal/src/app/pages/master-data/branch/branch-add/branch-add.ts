import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createBranch, type CreateBranchCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { BranchForm, type BranchFormValue } from "../components/branch-form/branch-form";

@Component({
  selector: "app-branch-add",
  templateUrl: "./branch-add.html",
  imports: [PageHeader, BranchForm],
})
export class BranchAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: BranchFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreateBranchCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      shortName: formValue.shortName,
      address: formValue.address ?? undefined,
      phone: formValue.phone,
      email: formValue.email,
      taxId: formValue.taxId,
    };

    try {
      const branch = await this.api.invoke(createBranch, { body: command });

      this.toastService.showSuccess(`Branch ${branch.code} created`);

      await this.router.navigateByUrl("/master-data/branches");
    } catch {
      this.toastService.showError(
        "Failed to create branch. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
