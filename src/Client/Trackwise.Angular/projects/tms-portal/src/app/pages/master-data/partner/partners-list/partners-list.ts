import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restorePartner, type BusinessPartnerDto } from "@trackwise/shared/api";
import {
  businessPartnerRoleOptions,
  masterDataApprovalStateOptions,
} from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { PartnersTable } from "../components/partners-table/partners-table";
import { PartnersListStore } from "../store/partners-list.store";

@Component({
  selector: "app-partners-list",
  templateUrl: "./partners-list.html",
  providers: [PartnersListStore],
  imports: [PartnersTable, Card, Grid, PageHeader, Stack, UiFormField, UiSelectField, UiTextField],
})
export class PartnersListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(PartnersListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedApprovalState = signal<string | null>(null);
  protected readonly selectedRole = signal<string | null>(null);
  protected readonly includeDeleted = signal(false);

  /**
   * The steward's list shows drafts and pending records - they are their own work in progress, and
   * a list that hid them would leave a submission with nowhere to be seen from.
   */
  protected readonly approvalStateOptions = [
    { label: "All Statuses", value: null },
    ...masterDataApprovalStateOptions,
  ];

  protected readonly roleOptions = [
    { label: "All Roles", value: null },
    ...businessPartnerRoleOptions,
  ];

  protected readonly deletedOptions = [
    { label: "Hide deleted", value: false },
    { label: "Include deleted", value: true },
  ];

  constructor() {
    // Seeded without a reload - the table fires its own initial lazy-load on mount, and reloading
    // here would make the page issue two identical requests.
    this.store.setFilters(
      { ApprovalState: undefined, Role: undefined, IncludeDeleted: false },
      { reload: false },
    );
  }

  protected addPartner(): void {
    void this.router.navigateByUrl("/master-data/partners/add");
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onApprovalStateChange(value: string | null): void {
    this.selectedApprovalState.set(value);
    this.store.setFilters({ ApprovalState: value ?? undefined });
  }

  protected onRoleChange(value: string | null): void {
    this.selectedRole.set(value);
    this.store.setFilters({ Role: value ?? undefined });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(partner: BusinessPartnerDto): Promise<void> {
    if (!partner.id) {
      return;
    }

    try {
      await this.api.invoke(restorePartner, { id: partner.id });
      this.toastService.showSuccess(`Partner ${partner.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore partner.");
    }
  }
}
