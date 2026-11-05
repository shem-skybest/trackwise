import { DatePipe } from "@angular/common";
import { Component, effect, inject, input, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import {
  Api,
  deleteMaintenanceSchedule,
  getMaintenanceRecords,
  getMaintenanceSchedules,
  updateMaintenanceSchedule,
  type MaintenanceRecordDto,
  type MaintenanceScheduleDto,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { LocalizationService, ToastService } from "@trackwise/shared/services";
import {
  Badge,
  Stack,
  Typography,
  UiButton,
  UiDataTable,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { MaintenanceScheduleDialog } from "../maintenance-schedule-dialog/maintenance-schedule-dialog";

const RECENT_SERVICES = 10;

/** One truck's maintenance: what's scheduled and when it's due, and the latest services logged. */
@Component({
  selector: "app-truck-maintenance",
  templateUrl: "./truck-maintenance.html",
  imports: [
    Badge,
    CurrencyFormatPipe,
    DatePipe,
    MaintenanceScheduleDialog,
    RouterLink,
    Stack,
    Typography,
    UiButton,
    UiDataTable,
  ],
})
export class TruckMaintenance {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  public readonly truckId = input.required<string>();
  public readonly truckNumber = input<string>("");

  protected readonly distanceUnit = inject(LocalizationService).getDistanceUnitLabel();
  protected readonly schedules = signal<MaintenanceScheduleDto[]>([]);
  protected readonly records = signal<MaintenanceRecordDto[]>([]);
  protected readonly totalRecords = signal(0);
  protected readonly isLoading = signal(false);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<MaintenanceScheduleDto | null>(null);

  constructor() {
    effect(() => {
      if (this.truckId()) {
        void this.refresh();
      }
    });
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.dialogVisible.set(true);
  }

  protected openEdit(schedule: MaintenanceScheduleDto): void {
    this.editing.set(schedule);
    this.dialogVisible.set(true);
  }

  protected logService(schedule?: MaintenanceScheduleDto): void {
    this.router.navigate(["/maintenance/records/add"], {
      queryParams: {
        truckId: this.truckId(),
        truckNumber: this.truckNumber(),
        type: schedule?.type,
        scheduleId: schedule?.id,
      },
    });
  }

  protected async setActive(schedule: MaintenanceScheduleDto, isActive: boolean): Promise<void> {
    await this.api.invoke(updateMaintenanceSchedule, {
      id: schedule.id!,
      body: {
        id: schedule.id!,
        intervalType: schedule.intervalType!,
        daysInterval: schedule.daysInterval,
        mileageInterval: schedule.mileageInterval,
        engineHoursInterval: schedule.engineHoursInterval,
        lastServiceDate: schedule.lastServiceDate,
        lastServiceOdometer: schedule.lastServiceOdometer,
        lastServiceEngineHours: schedule.lastServiceEngineHours,
        notes: schedule.notes,
        isActive,
      },
    });
    this.toast.showSuccess(isActive ? "Schedule resumed" : "Schedule paused");
    await this.refresh();
  }

  protected onDelete(schedule: MaintenanceScheduleDto): void {
    this.toast.confirm({
      header: "Delete schedule",
      message: `Delete the ${schedule.typeDisplay} schedule? Services already logged stay. To stop it for a while, pause it instead.`,
      acceptLabel: "Delete",
      rejectLabel: "Cancel",
      severity: "danger",
      accept: async () => {
        await this.api.invoke(deleteMaintenanceSchedule, { id: schedule.id! });
        this.toast.showSuccess("Schedule deleted");
        await this.refresh();
      },
    });
  }

  protected interval(schedule: MaintenanceScheduleDto): string {
    const parts = [
      schedule.daysInterval ? `${schedule.daysInterval} days` : null,
      schedule.mileageInterval
        ? `${schedule.mileageInterval.toLocaleString()} ${this.distanceUnit}`
        : null,
      schedule.engineHoursInterval ? `${schedule.engineHoursInterval} engine hrs` : null,
    ].filter(Boolean);
    return parts.length ? `Every ${parts.join(" or ")}` : "-";
  }

  protected dueLabel(schedule: MaintenanceScheduleDto): string {
    if (!schedule.isActive) {
      return "Paused";
    }
    if (schedule.isOverdue) {
      return `Overdue ${-(schedule.daysUntilDue ?? 0)} days`;
    }
    if (schedule.daysUntilDue === 0) {
      return "Due today";
    }
    return schedule.daysUntilDue != null ? `In ${schedule.daysUntilDue} days` : "Scheduled";
  }

  protected dueIntent(schedule: MaintenanceScheduleDto): UiBadgeIntent {
    if (!schedule.isActive) {
      return "secondary";
    }
    if (schedule.isOverdue) {
      return "danger";
    }
    return (schedule.daysUntilDue ?? 99) <= 7 ? "warn" : "info";
  }

  protected async refresh(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [schedules, records] = await Promise.all([
        this.api.invoke(getMaintenanceSchedules, {
          TruckId: this.truckId(),
          IncludeInactive: true,
        }),
        this.api.invoke(getMaintenanceRecords, {
          TruckId: this.truckId(),
          OrderBy: "-ServiceDate",
          PageSize: RECENT_SERVICES,
        }),
      ]);
      this.schedules.set(schedules ?? []);
      this.records.set(records?.items ?? []);
      this.totalRecords.set(records?.pagination?.total ?? records?.items?.length ?? 0);
    } finally {
      this.isLoading.set(false);
    }
  }
}
