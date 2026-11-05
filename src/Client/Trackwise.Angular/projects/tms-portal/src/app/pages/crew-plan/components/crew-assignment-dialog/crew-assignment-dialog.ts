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
import { form, FormField, FormRoot, maxLength, required, validate } from "@angular/forms/signals";
import { DRIVING_ROLES, getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  setCrewAssignment,
  type CrewPlanRowDto,
  type EmployeeDto,
  type HelperDto,
  type TruckDayStatus,
} from "@trackwise/shared/api";
import { truckDayStatusOptions } from "@trackwise/shared/api/enums";
import { ToastService } from "@trackwise/shared/services";
import {
  Stack,
  UiButton,
  UiDialog,
  UiFormField,
  UiSelectField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { SearchEmployee, SearchHelper } from "@/shared/components";

interface AssignmentModel {
  unitStatus: TruckDayStatus;
  driver: EmployeeDto | null;
  helper: HelperDto | null;
  secondHelper: HelperDto | null;
  notes: string;
}

/**
 * Plans one truck for one day: what the unit is doing and who rides it. An unplanned truck starts
 * from its usual driver, since that's who most often takes it.
 */
@Component({
  selector: "app-crew-assignment-dialog",
  templateUrl: "./crew-assignment-dialog.html",
  imports: [
    FormField,
    FormRoot,
    SearchEmployee,
    SearchHelper,
    Stack,
    UiButton,
    UiDialog,
    UiFormField,
    UiSelectField,
    UiTextareaField,
    ValidatedForm,
  ],
})
export class CrewAssignmentDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly visible = model<boolean>(false);
  public readonly row = input<CrewPlanRowDto | null>(null);
  public readonly saved = output<void>();

  protected readonly statusOptions = truckDayStatusOptions;
  protected readonly drivingRoles = DRIVING_ROLES;
  protected readonly title = computed(() => {
    const row = this.row();
    return row ? `${row.truckNumber} - ${row.date}` : "Plan truck";
  });

  protected readonly model = signal<AssignmentModel>(this.toModel(null));

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.unitStatus, { message: "Pick the unit's status for the day." });
      maxLength(p.notes, 500, { message: "Notes cannot exceed 500 characters." });
      validate(p.notes, ({ value, valueOf }) =>
        valueOf(p.unitStatus) === "other" && !value().trim()
          ? { kind: "notesRequired", message: "Say why the unit is off the road." }
          : null,
      );
      validate(p.secondHelper, ({ value, valueOf }) => {
        const first = valueOf(p.helper);
        const second = value();
        return first?.id && second?.id && first.id === second.id
          ? { kind: "sameHelper", message: "Pick a different second helper." }
          : null;
      });
    },
    {
      submission: {
        action: async () => {
          const row = this.row();
          if (!row) {
            return undefined;
          }

          const v = this.model();
          try {
            await this.api.invoke(setCrewAssignment, {
              body: {
                date: row.date,
                truckId: row.truckId,
                unitStatus: v.unitStatus,
                driverId: v.driver?.id ?? null,
                helperId: v.helper?.id ?? null,
                secondHelperId: v.secondHelper?.id ?? null,
                notes: v.notes.trim() || null,
              },
            });
            this.toast.showSuccess(`${row.truckNumber} planned`);
            this.saved.emit();
            this.visible.set(false);
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Couldn't save the plan."));
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const row = this.row();
      if (this.visible()) {
        // Untracked, so typing in the form doesn't re-run this and wipe what was entered.
        untracked(() => this.form().reset(this.toModel(row)));
      }
    });
  }

  protected close(): void {
    this.visible.set(false);
  }

  private toModel(row: CrewPlanRowDto | null): AssignmentModel {
    const planned = !!row?.assignmentId;
    const driverId = planned ? row?.driverId : row?.mainDriverId;
    const driverName = planned ? row?.driverName : row?.mainDriverName;
    return {
      unitStatus: row?.unitStatus ?? "available",
      driver: driverId ? ({ id: driverId, fullName: driverName } as EmployeeDto) : null,
      helper: row?.helperId ? ({ id: row.helperId, name: row.helperName } as HelperDto) : null,
      secondHelper: row?.secondHelperId
        ? ({ id: row.secondHelperId, name: row.secondHelperName } as HelperDto)
        : null,
      notes: row?.notes ?? "",
    };
  }
}
