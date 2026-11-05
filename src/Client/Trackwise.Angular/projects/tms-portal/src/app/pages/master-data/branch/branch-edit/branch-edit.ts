import { Component, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  deleteBranch,
  getBranchById,
  updateBranch,
  type BranchDto,
  type UpdateBranchCommand,
} from "@trackwise/shared/api";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import { BranchForm, type BranchFormValue } from "../components/branch-form/branch-form";

@Component({
  selector: "app-branch-edit",
  templateUrl: "./branch-edit.html",
  imports: [PageHeader, BranchForm, MasterDataHistory, Spinner, UiButton],
})
export class BranchEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly branch = signal<BranchDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);

  /** Bumped after a save so the history panel re-reads instead of showing a stale trail. */
  protected readonly historyVersion = signal(0);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    if (!this.id) {
      return;
    }

    this.isLoading.set(true);
    try {
      this.branch.set(await this.api.invoke(getBranchById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async update(formValue: BranchFormValue): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);

    // Code is absent by design - it is fixed after creation.
    const command: UpdateBranchCommand = {
      name: formValue.name,
      shortName: formValue.shortName,
      address: formValue.address ?? undefined,
      phone: formValue.phone,
      email: formValue.email,
      taxId: formValue.taxId,
      isActive: formValue.isActive,
    };

    try {
      await this.api.invoke(updateBranch, { id: this.id, body: command });

      this.toastService.showSuccess("Branch updated");

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch {
      this.toastService.showError(
        "Failed to update branch. Please check the information and try again.",
      );
    } finally {
      this.isBusy.set(false);
    }
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("branch", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deleteBranch, { id: this.id });

      this.toastService.showSuccess("Branch deleted. It can be restored from the branches list.");

      await this.router.navigateByUrl("/master-data/branches");
    } catch {
      this.toastService.showError("Failed to delete branch.");
    } finally {
      this.isBusy.set(false);
    }
  }
}
