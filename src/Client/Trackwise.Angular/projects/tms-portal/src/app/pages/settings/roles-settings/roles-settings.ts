import { Component, computed, inject, signal, type OnInit } from "@angular/core";
import { userRoleOptions } from "@trackwise/shared";
import {
  Api,
  deleteTenantRole,
  getPermissionCatalog,
  getRolePermissions,
  getTenantRoles,
  resetTenantRole,
  updateTenantRolePermissions,
  type PermissionGroupDto,
  type RoleDto,
} from "@trackwise/shared/api";
import {
  Alert,
  Badge,
  Card,
  Container,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiCheckboxField,
} from "@trackwise/shared/ui";
import { AuthService, PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { CopyRoleDialog } from "../components/copy-role-dialog/copy-role-dialog";
import { RoleHistoryCard } from "../components/role-history-card/role-history-card";

interface PermissionSection {
  name: string;
  groups: PermissionGroupDto[];
}

/**
 * Owner and IT decide what each role may do. One role at a time: tick the permissions, save.
 * The page only shapes the choices - the API re-checks every rule (no granting what you don't
 * hold, no editing your own role, Owner locked) and its refusal is shown as-is.
 */
@Component({
  selector: "app-roles-settings",
  templateUrl: "./roles-settings.html",
  imports: [
    Alert,
    Badge,
    Card,
    Container,
    CopyRoleDialog,
    PageHeader,
    RoleHistoryCard,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiCheckboxField,
  ],
})
export class RolesSettings implements OnInit {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);

  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly catalog = signal<PermissionGroupDto[]>([]);
  protected readonly selectedName = signal<string | null>(null);
  protected readonly saved = signal<ReadonlySet<string>>(new Set());
  protected readonly draft = signal<ReadonlySet<string>>(new Set());
  protected readonly copyDialogOpen = signal(false);
  protected readonly historyVersion = signal(0);

  private readonly myRole = this.authService.getUserData()?.role ?? null;

  protected readonly selected = computed(
    () => this.roles().find((r) => r.name === this.selectedName()) ?? null,
  );

  protected readonly sections = computed<PermissionSection[]>(() => {
    const bySection = new Map<string, PermissionGroupDto[]>();
    for (const group of this.catalog()) {
      const key = group.section ?? "Other";
      bySection.set(key, [...(bySection.get(key) ?? []), group]);
    }
    return [...bySection].map(([name, groups]) => ({ name, groups }));
  });

  protected readonly isDirty = computed(() => {
    const a = this.saved();
    const b = this.draft();
    return a.size !== b.size || [...a].some((p) => !b.has(p));
  });

  protected readonly readOnlyReason = computed(() => {
    const role = this.selected();
    if (!role || role.canEdit) return null;
    if (role.name === "tenant.owner") return "The Owner role always has every permission.";
    if (role.name === "tenant.customer") return "The Customer role belongs to the customer portal.";
    if (role.name === this.myRole)
      return "This is your own role. Ask the Owner or another admin to change it.";
    return "You can view this role but not change it.";
  });

  ngOnInit(): void {
    void this.load();
  }

  protected isMine(role: RoleDto): boolean {
    return role.name === this.myRole;
  }

  protected select(roleName: string): void {
    if (roleName === this.selectedName()) return;
    if (!this.isDirty()) {
      void this.open(roleName);
      return;
    }
    this.toast.confirm({
      header: "Unsaved changes",
      message: `Discard your changes to ${this.selected()?.displayName}?`,
      icon: "warning",
      severity: "warning",
      acceptLabel: "Discard",
      accept: () => void this.open(roleName),
    });
  }

  private async open(roleName: string): Promise<void> {
    this.selectedName.set(roleName);
    const permissions = await this.api.invoke(getRolePermissions, { roleName });
    const names = new Set(permissions.map((p) => p.name ?? "").filter(Boolean));
    this.saved.set(names);
    this.draft.set(names);
  }

  protected isChecked(permission: string): boolean {
    return this.draft().has(permission);
  }

  /** Basic permissions are always on; the rest must be within the editor's own reach. */
  protected isLocked(permission: string, isBasic: boolean): boolean {
    return (
      isBasic ||
      !this.selected()?.canEdit ||
      this.isSaving() ||
      !this.permissionService.hasPermission(permission)
    );
  }

  protected toggle(permission: string, checked: boolean): void {
    this.draft.update((current) => {
      const next = new Set(current);
      if (checked) next.add(permission);
      else next.delete(permission);
      return next;
    });
  }

  protected discard(): void {
    this.draft.set(this.saved());
  }

  protected async save(): Promise<void> {
    const role = this.selected();
    if (!role?.name) return;
    this.isSaving.set(true);
    try {
      await this.api.invoke(updateTenantRolePermissions, {
        roleName: role.name,
        body: { roleName: role.name, permissions: [...this.draft()] },
      });
      this.toast.showSuccess(`${role.displayName} saved. People in it get the change right away.`);
      await this.reloadRoles();
      await this.open(role.name);
    } catch {
      // The global error interceptor already shows the server's reason.
    } finally {
      this.isSaving.set(false);
    }
  }

  protected reset(): void {
    const role = this.selected();
    if (!role?.name) return;
    this.toast.confirm({
      header: "Reset to defaults",
      message: `Put ${role.displayName} back to its built-in permissions? Your changes to it are undone.`,
      icon: "refresh",
      severity: "warning",
      acceptLabel: "Reset",
      accept: () =>
        void this.runAndReload(
          role.name!,
          () => this.api.invoke(resetTenantRole, { roleName: role.name! }),
          `${role.displayName} is back to its defaults.`,
        ),
    });
  }

  protected remove(): void {
    const role = this.selected();
    if (!role?.name) return;
    this.toast.confirmDelete(`the role "${role.displayName}"`, () => {
      void this.runAndReload(
        null,
        () => this.api.invoke(deleteTenantRole, { roleName: role.name! }),
        `${role.displayName} deleted.`,
      );
    });
  }

  protected async onRoleCopied(roleName: string): Promise<void> {
    await this.reloadRoles();
    this.historyVersion.update((v) => v + 1);
    this.saved.set(new Set());
    this.draft.set(new Set());
    await this.open(roleName);
  }

  private async runAndReload(
    selectAfter: string | null,
    action: () => Promise<unknown>,
    successMessage: string,
  ): Promise<void> {
    this.isSaving.set(true);
    try {
      await action();
      this.toast.showSuccess(successMessage);
      await this.reloadRoles();
      this.saved.set(new Set());
      this.draft.set(new Set());
      const next = selectAfter ?? this.roles()[0]?.name ?? null;
      if (next) await this.open(next);
    } catch {
      // The global error interceptor already shows the server's reason.
    } finally {
      this.isSaving.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      const [catalog] = await Promise.all([
        this.api.invoke(getPermissionCatalog, {}),
        this.reloadRoles(),
      ]);
      this.catalog.set(catalog);
      const first = this.roles().find((r) => r.canEdit) ?? this.roles()[0];
      if (first?.name) await this.open(first.name);
    } catch {
      this.toast.showError("Failed to load roles");
    } finally {
      this.isLoading.set(false);
    }
  }

  private async reloadRoles(): Promise<void> {
    const result = await this.api.invoke(getTenantRoles, { Page: 1, PageSize: 100 });
    const roles = (result.items ?? []).filter((r) => r.name !== "tenant.customer");
    // Built-in roles in the order the company is organised, then roles made in Settings.
    const order = (role: RoleDto) => {
      const index = userRoleOptions.findIndex((o) => o.value === role.name);
      return index < 0 ? Number.MAX_SAFE_INTEGER : index;
    };
    this.roles.set(
      [...roles].sort(
        (a, b) => order(a) - order(b) || (a.displayName ?? "").localeCompare(b.displayName ?? ""),
      ),
    );
    this.historyVersion.update((v) => v + 1);
  }
}
