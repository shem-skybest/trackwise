import { Component, computed, effect, inject, signal } from "@angular/core";
import { getApiErrorMessage, PageHeader } from "@trackwise/shared";
import {
  Api,
  copyCrewPlan,
  deleteCrewAssignment,
  getCrewPlan,
  type CrewPlanRowDto,
  type TruckDayStatus,
  type TruckType,
} from "@trackwise/shared/api";
import {
  truckDayStatusOptions,
  truckTonnageClassOptions,
  truckTypeOptions,
} from "@trackwise/shared/api/enums";
import { ToastService } from "@trackwise/shared/services";
import {
  Badge,
  Card,
  CountBadge,
  Grid,
  Icon,
  Stack,
  StatItem,
  UiButton,
  UiDataTable,
  UiDateField,
  UiFormField,
  UiSelectField,
  UiTabsImports,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { dateToIso } from "@/shared/utils";
import { CrewAssignmentDialog } from "../components/crew-assignment-dialog/crew-assignment-dialog";
import { CrewDayPanel } from "../components/crew-day-panel/crew-day-panel";
import { UnassignedBookingsPanel } from "../components/unassigned-bookings-panel/unassigned-bookings-panel";

type PlanTab = "trucks" | "crew" | "bookings";

const OFF_ROAD: readonly TruckDayStatus[] = [
  "for_repair",
  "under_repair",
  "maintenance",
  "stuck",
  "for_transfer",
  "other",
];

const STATUS_INTENT: Record<TruckDayStatus, UiBadgeIntent> = {
  available: "success",
  buffer: "info",
  understaffed: "warn",
  for_repair: "danger",
  under_repair: "danger",
  maintenance: "warn",
  stuck: "danger",
  for_transfer: "secondary",
  other: "secondary",
};

/**
 * The dispatcher's morning plan: every truck for one day, its status and crew. Replaces the rows
 * of Monday's CCL OPS - DISPATCH board; trips created on a truck that day take its crew from here.
 * The Crew tab holds each person's status for the day (available, reserve, absent...); the Bookings
 * tab lists the day's Cold Chain bookings that still have no trip.
 */
@Component({
  selector: "app-crew-plan-list",
  templateUrl: "./crew-plan-list.html",
  imports: [
    Badge,
    Card,
    CountBadge,
    CrewAssignmentDialog,
    CrewDayPanel,
    Grid,
    Icon,
    PageHeader,
    Stack,
    StatItem,
    UiButton,
    UiDataTable,
    UiDateField,
    UiFormField,
    UiSelectField,
    UiTabsImports,
    UnassignedBookingsPanel,
  ],
})
export class CrewPlanList {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  protected readonly typeOptions: { label: string; value: TruckType | null }[] = [
    { label: "All trucks", value: null },
    ...truckTypeOptions,
  ];

  protected readonly date = signal<Date | null>(startOfToday());
  protected readonly isoDate = computed(() => dateToIso(this.date()));
  protected readonly tab = signal<PlanTab>("trucks");
  /** Bumped when a truck's crew is saved, so the crew tab's truck column re-reads. */
  protected readonly planVersion = signal(0);
  /** Bumped by Refresh, so the bookings tab re-reads too. */
  protected readonly refreshVersion = signal(0);
  protected readonly unassignedCount = signal<number | null>(null);
  protected readonly truckType = signal<TruckType | null>("reefer");
  protected readonly plan = signal<CrewPlanRowDto[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<CrewPlanRowDto | null>(null);

  protected readonly rows = computed(() =>
    this.plan().map((row) => ({
      row,
      tonnage: optionLabel(truckTonnageClassOptions, row.tonnageClass),
      status: row.unitStatus
        ? {
            label: optionLabel(truckDayStatusOptions, row.unitStatus),
            intent: STATUS_INTENT[row.unitStatus],
          }
        : null,
      helpers: [row.helperName, row.secondHelperName].filter(Boolean).join(", "),
    })),
  );

  protected readonly availableCount = computed(
    () => this.plan().filter((r) => r.unitStatus === "available").length,
  );
  protected readonly understaffedCount = computed(
    () => this.plan().filter((r) => r.unitStatus === "understaffed").length,
  );
  protected readonly offRoadCount = computed(
    () => this.plan().filter((r) => r.unitStatus && OFF_ROAD.includes(r.unitStatus)).length,
  );
  protected readonly unplannedCount = computed(
    () => this.plan().filter((r) => !r.assignmentId).length,
  );

  constructor() {
    effect(() => {
      const date = this.date();
      const truckType = this.truckType();
      if (date) {
        void this.load(date, truckType);
      }
    });
  }

  protected shiftDay(days: number): void {
    const current = this.date() ?? startOfToday();
    this.date.set(new Date(current.getFullYear(), current.getMonth(), current.getDate() + days));
  }

  protected selectTab(value: unknown): void {
    this.tab.set(value === "crew" || value === "bookings" ? value : "trucks");
  }

  protected onPlanSaved(): void {
    this.refresh();
    this.planVersion.update((v) => v + 1);
  }

  protected goToToday(): void {
    this.date.set(startOfToday());
  }

  protected openEdit(row: CrewPlanRowDto): void {
    this.editing.set(row);
    this.dialogVisible.set(true);
  }

  protected confirmClear(row: CrewPlanRowDto): void {
    this.toast.confirm({
      message: `Clear ${row.truckNumber}'s plan for ${row.date}? Trips already created keep their crew.`,
      header: "Clear plan",
      icon: "warning",
      severity: "danger",
      accept: () => void this.clear(row),
    });
  }

  protected copyPreviousDay(): void {
    const date = this.date();
    if (!date) {
      return;
    }

    const previous = new Date(date.getFullYear(), date.getMonth(), date.getDate() - 1);
    this.toast.confirm({
      message:
        `Copy ${dateToIso(previous)}'s crews to ${dateToIso(date)}? Trucks already planned ` +
        "for this day are left as they are.",
      header: "Copy previous day",
      accept: () => void this.copy(previous, date),
    });
  }

  protected refresh(): void {
    const date = this.date();
    if (date) {
      void this.load(date, this.truckType());
      this.refreshVersion.update((v) => v + 1);
    }
  }

  private async load(date: Date, truckType: TruckType | null): Promise<void> {
    this.isLoading.set(true);
    try {
      const rows = await this.api.invoke(getCrewPlan, {
        Date: dateToIso(date)!,
        TruckType: truckType ?? undefined,
      });
      this.plan.set(rows ?? []);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the crew plan."));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async clear(row: CrewPlanRowDto): Promise<void> {
    try {
      await this.api.invoke(deleteCrewAssignment, { id: row.assignmentId! });
      this.toast.showSuccess(`${row.truckNumber}'s plan cleared`);
      this.refresh();
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't clear the plan."));
    }
  }

  private async copy(from: Date, to: Date): Promise<void> {
    try {
      const copied = await this.api.invoke(copyCrewPlan, {
        body: { fromDate: dateToIso(from)!, toDate: dateToIso(to)! },
      });
      this.toast.showSuccess(
        copied ? `Copied ${copied} truck${copied === 1 ? "" : "s"}` : "Nothing new to copy",
      );
      this.refresh();
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't copy the plan."));
    }
  }
}

function startOfToday(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

function optionLabel<T>(
  options: { label: string; value: T }[],
  value: T | null | undefined,
): string {
  return options.find((o) => o.value === value)?.label ?? "";
}
