import { Component, computed, inject, input, output, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { Permission } from "@trackwise/shared";
import type {
  FreightMode,
  TruckLegDto,
  TruckLegRole,
  WorkfileContainerDto,
  WorkfileStatus,
} from "@trackwise/shared/api";
import { truckLegRoleOptions, workfileStatusOptions } from "@trackwise/shared/api/enums";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Stack,
  Typography,
  UiButton,
  UiDataTable,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { WorkfileLegDialog } from "../workfile-leg-dialog/workfile-leg-dialog";

const STATUS_INTENT: Partial<Record<WorkfileStatus, UiBadgeIntent>> = {
  open: "info",
  manager_review: "warn",
  accounting_review: "warn",
  closed: "success",
  cancelled: "danger",
};

/**
 * A forwarding workfile's truck legs - the trucking that moves its boxes to or from the port. Each
 * leg is its own SLC-TRK file; the client is billed once, on this workfile.
 */
@Component({
  selector: "app-workfile-legs",
  templateUrl: "./workfile-legs.html",
  imports: [
    Badge,
    Card,
    DateFormatPipe,
    RouterLink,
    Stack,
    Typography,
    UiButton,
    UiDataTable,
    WorkfileLegDialog,
  ],
})
export class WorkfileLegs {
  private readonly permissionService = inject(PermissionService);

  public readonly workfileId = input.required<string>();
  public readonly customerId = input.required<string>();
  public readonly direction = input<FreightMode | null | undefined>(null);
  public readonly legs = input.required<TruckLegDto[]>();
  public readonly containers = input.required<WorkfileContainerDto[]>();
  public readonly editable = input(false);
  public readonly changed = output();

  protected readonly dialogOpen = signal(false);
  protected readonly editing = signal<TruckLegDto | null>(null);

  protected readonly canManage = computed(
    () => this.editable() && this.permissionService.hasPermission(Permission.Workfile.Manage),
  );

  protected readonly defaultRole = computed<TruckLegRole>(() =>
    this.direction() === "export" ? "pickup" : "delivery",
  );

  protected openAdd(): void {
    this.editing.set(null);
    this.dialogOpen.set(true);
  }

  protected openEdit(leg: TruckLegDto): void {
    this.editing.set(leg);
    this.dialogOpen.set(true);
  }

  protected roleLabel(role: TruckLegRole | undefined): string {
    return truckLegRoleOptions.find((o) => o.value === role)?.label.split(" (")[0] ?? "-";
  }

  protected statusLabel(status: WorkfileStatus | undefined): string {
    return workfileStatusOptions.find((o) => o.value === status)?.label ?? "-";
  }

  protected statusIntent(status: WorkfileStatus | undefined): UiBadgeIntent {
    return (status && STATUS_INTENT[status]) || "secondary";
  }
}
