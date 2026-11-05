import { DecimalPipe } from "@angular/common";
import { Component, computed, inject, input, output, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { getApiErrorMessage, Permission } from "@trackwise/shared";
import {
  Api,
  removeWorkfileContainer,
  type ContainerMoveStatus,
  type WorkfileContainerDto,
} from "@trackwise/shared/api";
import { containerMoveStatusOptions } from "@trackwise/shared/api/enums";
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
import { ToastService } from "@/core/services";
import { WorkfileContainerDialog } from "../workfile-container-dialog/workfile-container-dialog";

const MOVE_INTENT: Record<ContainerMoveStatus, UiBadgeIntent> = {
  not_started: "secondary",
  loading: "warn",
  on_the_road: "info",
  unloading: "warn",
  at_port: "warn",
  on_vessel: "info",
  at_yard: "secondary",
  delivered: "success",
};

const DAY_MS = 24 * 60 * 60 * 1000;

/** The container slots on a workfile: sizes first, box numbers and seals as they become known. */
@Component({
  selector: "app-workfile-containers",
  templateUrl: "./workfile-containers.html",
  imports: [
    Badge,
    Card,
    DecimalPipe,
    RouterLink,
    Stack,
    Typography,
    UiButton,
    UiDataTable,
    WorkfileContainerDialog,
  ],
})
export class WorkfileContainers {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly permissionService = inject(PermissionService);

  public readonly workfileId = input.required<string>();
  public readonly containers = input.required<WorkfileContainerDto[]>();
  public readonly editable = input(false);
  /** Forwarding files show where each box is on the move; other files have no legs to say. */
  public readonly showMoveStatus = input(false);
  public readonly changed = output();

  protected readonly dialogOpen = signal(false);
  protected readonly editing = signal<WorkfileContainerDto | null>(null);

  protected readonly canManage = computed(
    () => this.editable() && this.permissionService.hasPermission(Permission.Workfile.Manage),
  );

  /** "3 x HC40, 1 x GP20" - how a forwarding job is booked and talked about. */
  protected readonly summary = computed(() => {
    const counts = new Map<string, number>();
    for (const slot of this.containers()) {
      const code = slot.containerTypeCode ?? "?";
      counts.set(code, (counts.get(code) ?? 0) + 1);
    }
    return [...counts].map(([code, n]) => `${n} x ${code}`).join(", ");
  });

  protected readonly numbered = computed(
    () => this.containers().filter((slot) => slot.containerNumber).length,
  );

  protected moveLabel(slot: WorkfileContainerDto): string {
    return containerMoveStatusOptions.find((o) => o.value === slot.moveStatus)?.label ?? "-";
  }

  protected moveIntent(slot: WorkfileContainerDto): UiBadgeIntent {
    return (slot.moveStatus && MOVE_INTENT[slot.moveStatus]) || "secondary";
  }

  /** How long a box has sat at the port - what demurrage and storage are charged on. */
  protected daysAtPort(slot: WorkfileContainerDto): number | null {
    if (slot.moveStatus !== "at_port" || !slot.moveStatusSince) {
      return null;
    }
    return Math.max(
      0,
      Math.floor((Date.now() - new Date(slot.moveStatusSince).getTime()) / DAY_MS),
    );
  }

  protected openAdd(): void {
    this.editing.set(null);
    this.dialogOpen.set(true);
  }

  protected openEdit(slot: WorkfileContainerDto): void {
    this.editing.set(slot);
    this.dialogOpen.set(true);
  }

  protected confirmRemove(slot: WorkfileContainerDto): void {
    const name = slot.containerNumber ?? `this ${slot.containerTypeCode ?? ""} slot`;
    this.toast.confirm({
      header: "Remove container",
      message: `Remove ${name} from this workfile? The box stays in the container register.`,
      icon: "warning",
      severity: "danger",
      acceptLabel: "Remove",
      accept: () => void this.remove(slot),
    });
  }

  private async remove(slot: WorkfileContainerDto): Promise<void> {
    try {
      await this.api.invoke(removeWorkfileContainer, {
        id: this.workfileId(),
        containerId: slot.id!,
      });
      this.toast.showSuccess("Container removed.");
      this.changed.emit();
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Could not remove the container."));
    }
  }
}
