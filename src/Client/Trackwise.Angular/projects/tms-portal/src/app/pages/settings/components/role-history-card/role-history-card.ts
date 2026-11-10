import { DatePipe } from "@angular/common";
import { Component, effect, inject, input, signal, untracked } from "@angular/core";
import { Api, getTenantRoleChangeLog, type TenantRoleChangeLogDto } from "@trackwise/shared/api";
import { Card, Spinner, Stack, UiButton } from "@trackwise/shared/ui";

const PAGE_SIZE = 20;

/** Who changed which role, or moved whom, newest first. `version` bumps make it reload. */
@Component({
  selector: "app-role-history-card",
  templateUrl: "./role-history-card.html",
  imports: [Card, DatePipe, Spinner, Stack, UiButton],
})
export class RoleHistoryCard {
  private readonly api = inject(Api);

  public readonly version = input(0);

  protected readonly entries = signal<TenantRoleChangeLogDto[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly hasMore = signal(false);
  private page = 1;

  constructor() {
    effect(() => {
      this.version();
      untracked(() => void this.load(true));
    });
  }

  protected loadMore(): void {
    void this.load(false);
  }

  protected describe(entry: TenantRoleChangeLogDto): string {
    const role = entry.roleDisplayName ?? entry.roleName;
    switch (entry.changeType) {
      case "PermissionAdded":
        return `gave ${role}: ${this.permissionLabel(entry.permission)}`;
      case "PermissionRemoved":
        return `took from ${role}: ${this.permissionLabel(entry.permission)}`;
      case "RoleCreated":
        return `created the role ${role}`;
      case "RoleDeleted":
        return `deleted the role ${role}`;
      case "ResetToDefaults":
        return `reset ${role} to its defaults`;
      case "EmployeeAssigned":
        return entry.previousRoleName
          ? `moved ${entry.employeeName} from ${entry.previousRoleName} to ${role}`
          : `put ${entry.employeeName} in ${role}`;
      default:
        return `changed ${role}`;
    }
  }

  /** "Permission.Booking.ViewCosts" -> "Booking · ViewCosts" - the log keeps the raw name on purpose. */
  private permissionLabel(permission: string | null | undefined): string {
    const [, module, verb] = (permission ?? "").split(".");
    return module && verb ? `${module} · ${verb}` : (permission ?? "");
  }

  private async load(fromStart: boolean): Promise<void> {
    this.isLoading.set(true);
    try {
      this.page = fromStart ? 1 : this.page + 1;
      const result = await this.api.invoke(getTenantRoleChangeLog, {
        Page: this.page,
        PageSize: PAGE_SIZE,
      });
      const items = result.items ?? [];
      this.entries.set(fromStart ? items : [...this.entries(), ...items]);
      this.hasMore.set(items.length === PAGE_SIZE);
    } catch {
      // History is secondary - leave what is shown; the interceptor reports the failure.
    } finally {
      this.isLoading.set(false);
    }
  }
}
