import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { BranchDto } from "@trackwise/shared/api";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

@Component({
  selector: "app-branches-table",
  templateUrl: "./branches-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class BranchesTable {
  private readonly router = inject(Router);

  public readonly data = input.required<BranchDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addBranch = output<void>();
  public readonly restoreBranch = output<BranchDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddBranch(): void {
    this.addBranch.emit();
  }

  protected navigateToBranch(branch: BranchDto): void {
    // A deleted branch has no edit form - restore is the only action available on it.
    if (branch.id && !branch.deletedAt) {
      this.router.navigateByUrl(`/master-data/branches/${branch.id}`);
    }
  }

  protected onRestore(branch: BranchDto, event: Event): void {
    event.stopPropagation();
    this.restoreBranch.emit(branch);
  }
}
