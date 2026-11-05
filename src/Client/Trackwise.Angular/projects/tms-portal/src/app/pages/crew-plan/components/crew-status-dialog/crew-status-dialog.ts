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
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  setCrewStatus,
  type CrewDayMemberDto,
  type CrewDayStatus,
} from "@trackwise/shared/api";
import { crewDayStatusOptions } from "@trackwise/shared/api/enums";
import { ToastService } from "@trackwise/shared/services";
import {
  Stack,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiSelectField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { dateToIso, isoToDate } from "@/shared/utils";

interface StatusModel {
  status: CrewDayStatus;
  until: Date | null;
  notes: string;
}

/** Longer than this is a roster change, not a day status - the API refuses it too. */
const MAX_DAYS = 62;

/**
 * Sets one driver's or helper's status for the plan's day, or through a later day for leave and
 * suspensions. Absent, on leave and suspended also take them off any truck they were planned on.
 */
@Component({
  selector: "app-crew-status-dialog",
  templateUrl: "./crew-status-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Stack,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiSelectField,
    UiTextareaField,
    ValidatedForm,
  ],
})
export class CrewStatusDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly visible = model<boolean>(false);
  public readonly member = input<CrewDayMemberDto | null>(null);
  /** The plan's day, as an ISO date. */
  public readonly date = input.required<string>();
  public readonly saved = output<void>();

  protected readonly statusOptions = crewDayStatusOptions;
  protected readonly title = computed(() => `${this.member()?.name ?? "Crew"} - ${this.date()}`);

  protected readonly model = signal<StatusModel>({ status: "available", until: null, notes: "" });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.status, { message: "Pick a status." });
      maxLength(p.notes, 500, { message: "Notes cannot exceed 500 characters." });
      validate(p.until, ({ value }) => {
        const until = dateToIso(value());
        if (!until) {
          return null;
        }
        const days = daysBetween(this.date(), until);
        if (days < 0) {
          return { kind: "untilBeforeDay", message: "The last day can't be before this day." };
        }
        return days >= MAX_DAYS
          ? { kind: "tooLong", message: `Set at most ${MAX_DAYS} days at once.` }
          : null;
      });
    },
    {
      submission: {
        action: async () => {
          const member = this.member();
          if (!member?.personId) {
            return undefined;
          }

          const v = this.model();
          try {
            const change = await this.api.invoke(setCrewStatus, {
              body: {
                personId: member.personId,
                date: this.date(),
                untilDate: dateToIso(v.until),
                status: v.status,
                notes: v.notes.trim() || null,
              },
            });
            const removed = change?.removedFrom ?? [];
            this.toast.showSuccess(
              removed.length
                ? `${member.name} taken off ${removed.join(", ")}`
                : `${member.name}'s status saved`,
            );
            this.saved.emit();
            this.visible.set(false);
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Couldn't save the status."));
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const member = this.member();
      if (this.visible()) {
        // Untracked, so typing in the form doesn't re-run this and wipe what was entered.
        untracked(() =>
          this.form().reset({
            status: member?.status ?? "available",
            until: null,
            notes: member?.notes ?? "",
          }),
        );
      }
    });
  }

  protected close(): void {
    this.visible.set(false);
  }
}

function daysBetween(fromIso: string, toIso: string): number {
  const from = isoToDate(fromIso);
  const to = isoToDate(toIso);
  return from && to ? Math.round((to.getTime() - from.getTime()) / 86_400_000) : 0;
}
