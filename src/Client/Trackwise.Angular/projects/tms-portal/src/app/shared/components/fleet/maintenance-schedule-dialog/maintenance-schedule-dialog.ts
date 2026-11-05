import {
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from "@angular/core";
import {
  disabled,
  form,
  FormField,
  FormRoot,
  maxLength,
  min,
  required,
  validate,
} from "@angular/forms/signals";
import {
  Api,
  createMaintenanceSchedule,
  updateMaintenanceSchedule,
  type MaintenanceIntervalType,
  type MaintenanceScheduleDto,
  type MaintenanceType,
} from "@trackwise/shared/api";
import {
  maintenanceIntervalTypeOptions,
  maintenanceTypeOptions,
} from "@trackwise/shared/api/enums";
import { LocalizationService, ToastService } from "@trackwise/shared/services";
import {
  Stack,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";

interface ScheduleModel {
  type: MaintenanceType;
  intervalType: MaintenanceIntervalType;
  daysInterval: number | null;
  mileageInterval: number | null;
  engineHoursInterval: number | null;
  lastServiceDate: Date | null;
  lastServiceOdometer: number | null;
  lastServiceEngineHours: number | null;
  notes: string;
}

/** Defaults to the weekly reefer PMS Cold Chain runs. */
const EMPTY: ScheduleModel = {
  type: "reefer_pms",
  intervalType: "time_based",
  daysInterval: 7,
  mileageInterval: null,
  engineHoursInterval: null,
  lastServiceDate: null,
  lastServiceOdometer: null,
  lastServiceEngineHours: null,
  notes: "",
};

/**
 * Sets up or changes how often a truck's service comes round. Logging that service later moves
 * the schedule on by itself; the last service here is only for setting it up or correcting it.
 */
@Component({
  selector: "app-maintenance-schedule-dialog",
  templateUrl: "./maintenance-schedule-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Stack,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    ValidatedForm,
  ],
})
export class MaintenanceScheduleDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly visible = model<boolean>(false);
  public readonly truckId = input.required<string>();
  /** The schedule to change, or null to set up a new one. */
  public readonly schedule = input<MaintenanceScheduleDto | null>(null);
  public readonly saved = output<void>();

  protected readonly typeOptions = maintenanceTypeOptions;
  protected readonly intervalOptions = maintenanceIntervalTypeOptions;
  protected readonly distanceUnit = inject(LocalizationService).getDistanceUnitLabel();
  protected readonly title = computed(() => (this.schedule() ? "Edit schedule" : "Add schedule"));
  protected readonly model = signal<ScheduleModel>({ ...EMPTY });

  protected readonly usesDays = computed(() => this.uses("time_based"));
  protected readonly usesDistance = computed(() => this.uses("mileage"));
  protected readonly usesHours = computed(() => this.uses("engine_hours"));

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.type, { message: "Pick the service." });
      disabled(p.type, { when: () => this.schedule() !== null });
      required(p.intervalType, { message: "Pick how it repeats." });
      min(p.daysInterval, 1, { message: "At least 1 day." });
      min(p.mileageInterval, 1, { message: "Greater than zero." });
      min(p.engineHoursInterval, 1, { message: "Greater than zero." });
      validate(p.daysInterval, ({ value }) =>
        this.model().intervalType === "time_based" && !value()
          ? { kind: "required", message: "How many days between services?" }
          : null,
      );
      validate(p.mileageInterval, ({ value }) =>
        this.model().intervalType === "mileage" && !value()
          ? { kind: "required", message: `How many ${this.distanceUnit} between services?` }
          : null,
      );
      validate(p.engineHoursInterval, ({ value }) =>
        this.model().intervalType === "engine_hours" && !value()
          ? { kind: "required", message: "How many engine hours between services?" }
          : null,
      );
      maxLength(p.notes, 1000, { message: "Keep notes under 1000 characters." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const body = {
            intervalType: v.intervalType,
            daysInterval: this.usesDays() ? v.daysInterval : null,
            mileageInterval: this.usesDistance() ? v.mileageInterval : null,
            engineHoursInterval: this.usesHours() ? v.engineHoursInterval : null,
            lastServiceDate: v.lastServiceDate?.toISOString() ?? null,
            lastServiceOdometer: v.lastServiceOdometer,
            lastServiceEngineHours: v.lastServiceEngineHours,
            notes: v.notes.trim() || null,
          };

          const existing = this.schedule();
          const result = existing?.id
            ? await this.api.invoke(updateMaintenanceSchedule, {
                id: existing.id,
                body: { ...body, id: existing.id, isActive: existing.isActive ?? true },
              })
            : await this.api.invoke(createMaintenanceSchedule, {
                body: { ...body, truckId: this.truckId(), type: v.type },
              });

          if (result) {
            this.toast.showSuccess(existing ? "Schedule updated" : "Schedule added");
            this.saved.emit();
            this.visible.set(false);
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const schedule = this.schedule();
      if (this.visible()) {
        // Untracked, so typing in the form doesn't re-run this and wipe what was entered.
        untracked(() => this.form().reset(schedule ? this.toModel(schedule) : { ...EMPTY }));
      }
    });
  }

  protected close(): void {
    this.visible.set(false);
  }

  private uses(kind: MaintenanceIntervalType): boolean {
    const type = this.model().intervalType;
    return type === kind || type === "combined";
  }

  private toModel(schedule: MaintenanceScheduleDto): ScheduleModel {
    return {
      type: schedule.type ?? "reefer_pms",
      intervalType: schedule.intervalType ?? "time_based",
      daysInterval: schedule.daysInterval ?? null,
      mileageInterval: schedule.mileageInterval ?? null,
      engineHoursInterval: schedule.engineHoursInterval ?? null,
      lastServiceDate: schedule.lastServiceDate ? new Date(schedule.lastServiceDate) : null,
      lastServiceOdometer: schedule.lastServiceOdometer ?? null,
      lastServiceEngineHours: schedule.lastServiceEngineHours ?? null,
      notes: schedule.notes ?? "",
    };
  }
}
