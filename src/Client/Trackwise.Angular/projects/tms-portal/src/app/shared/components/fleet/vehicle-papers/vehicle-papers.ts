import { DatePipe } from "@angular/common";
import { Component, computed, effect, inject, input, signal } from "@angular/core";
import {
  Api,
  deleteVehiclePermit,
  getVehiclePermits,
  type VehiclePermitDto,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { ToastService } from "@trackwise/shared/services";
import { Badge, Stack, Typography, UiButton, UiDataTable } from "@trackwise/shared/ui";
import { permitExpiry, permitTypeLabel } from "../permit-expiry";
import { VehiclePermitDialog } from "../vehicle-permit-dialog/vehicle-permit-dialog";

/** One truck's or trailer's papers, current and past, with add / correct / delete. Give exactly one id. */
@Component({
  selector: "app-vehicle-papers",
  templateUrl: "./vehicle-papers.html",
  imports: [
    Badge,
    CurrencyFormatPipe,
    DatePipe,
    Stack,
    Typography,
    UiButton,
    UiDataTable,
    VehiclePermitDialog,
  ],
})
export class VehiclePapers {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly truckId = input<string | null>(null);
  public readonly trailerId = input<string | null>(null);

  protected readonly permits = signal<VehiclePermitDto[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<VehiclePermitDto | null>(null);

  protected readonly rows = computed(() =>
    this.permits().map((permit) => ({
      permit,
      typeLabel: permitTypeLabel(permit),
      expiry: permitExpiry(permit),
    })),
  );

  constructor() {
    effect(() => {
      if (this.truckId() || this.trailerId()) {
        void this.refresh();
      }
    });
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.dialogVisible.set(true);
  }

  protected openEdit(permit: VehiclePermitDto): void {
    this.editing.set(permit);
    this.dialogVisible.set(true);
  }

  protected onDelete(permit: VehiclePermitDto): void {
    this.toast.confirm({
      header: "Delete paper",
      message: `Delete this ${permitTypeLabel(permit)}? Record a renewal as a new paper instead of deleting the old one.`,
      acceptLabel: "Delete",
      rejectLabel: "Cancel",
      severity: "danger",
      accept: async () => {
        try {
          await this.api.invoke(deleteVehiclePermit, { id: permit.id! });
          this.toast.showSuccess("Paper deleted");
          await this.refresh();
        } catch {
          this.toast.showError("Failed to delete the paper.");
        }
      },
    });
  }

  protected async refresh(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.api.invoke(getVehiclePermits, {
        TruckId: this.truckId() ?? undefined,
        TrailerId: this.trailerId() ?? undefined,
      });
      // Newest first here: the current paper on top, its history below.
      this.permits.set([...(result ?? [])].reverse());
    } finally {
      this.isLoading.set(false);
    }
  }
}
