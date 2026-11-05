import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  getCrewDay,
  type CrewDayMemberDto,
  type CrewDayStatus,
  type CrewMemberKind,
} from "@trackwise/shared/api";
import {
  crewDayStatusOptions,
  crewUnitOptions,
  employmentTypeOptions,
} from "@trackwise/shared/api/enums";
import { ToastService } from "@trackwise/shared/services";
import {
  Badge,
  Stack,
  UiButton,
  UiDataTable,
  UiFormField,
  UiSelectField,
  UiTextField,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { CrewStatusDialog } from "../crew-status-dialog/crew-status-dialog";

const STATUS_INTENT: Record<CrewDayStatus, UiBadgeIntent> = {
  available: "success",
  reserve: "info",
  absent: "danger",
  on_leave: "warn",
  suspended: "danger",
  training: "secondary",
  lent_out: "secondary",
};

const NOT_WORKING: readonly CrewDayStatus[] = ["absent", "on_leave", "suspended"];
const ELSEWHERE: readonly CrewDayStatus[] = ["training", "lent_out"];

/**
 * The day's crew: every driver and helper, whether they're working and which truck they're on.
 * Replaces the status suffixes Monday's dispatchers typed into crew names ("ANDRES ( ABSENT )").
 */
@Component({
  selector: "app-crew-day-panel",
  templateUrl: "./crew-day-panel.html",
  imports: [
    Badge,
    CrewStatusDialog,
    Stack,
    UiButton,
    UiDataTable,
    UiFormField,
    UiSelectField,
    UiTextField,
  ],
})
export class CrewDayPanel {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  /** The plan's day, as an ISO date. */
  public readonly date = input.required<string>();
  /** Bumped by the page when the truck plan changes, so the truck column stays current. */
  public readonly reloadKey = input(0);
  /** Raised after a status change, which can take someone off a truck. */
  public readonly changed = output<void>();

  protected readonly kindOptions: { label: string; value: CrewMemberKind | "all" }[] = [
    { label: "Drivers and helpers", value: "all" },
    { label: "Drivers", value: "driver" },
    { label: "Helpers", value: "helper" },
  ];

  protected readonly crew = signal<CrewDayMemberDto[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly kind = signal<CrewMemberKind | "all">("all");
  protected readonly search = signal("");
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<CrewDayMemberDto | null>(null);

  protected readonly rows = computed(() => {
    const kind = this.kind();
    const search = this.search().trim().toLowerCase();
    return this.crew()
      .filter((m) => kind === "all" || m.kind === kind)
      .filter((m) => !search || (m.name ?? "").toLowerCase().includes(search))
      .map((member) => ({
        member,
        status: {
          label: optionLabel(crewDayStatusOptions, member.status),
          intent: STATUS_INTENT[member.status ?? "available"],
        },
        employment: optionLabel(employmentTypeOptions, member.employmentType),
        unit: optionLabel(crewUnitOptions, member.homeUnit),
      }));
  });

  protected readonly counts = computed(() => {
    // Inactive crew are listed only on a day they have a status or a truck, so they count too.
    const crew = this.crew();
    const count = (statuses: readonly CrewDayStatus[]) =>
      crew.filter((m) => statuses.includes(m.status ?? "available")).length;
    return {
      available: count(["available"]),
      reserve: count(["reserve"]),
      notWorking: count(NOT_WORKING),
      elsewhere: count(ELSEWHERE),
      onTrucks: crew.filter((m) => m.truckId).length,
    };
  });

  constructor() {
    effect(() => {
      this.reloadKey();
      void this.load(this.date());
    });
  }

  protected openEdit(member: CrewDayMemberDto): void {
    this.editing.set(member);
    this.dialogVisible.set(true);
  }

  protected onSaved(): void {
    void this.load(this.date());
    this.changed.emit();
  }

  private async load(date: string): Promise<void> {
    this.isLoading.set(true);
    try {
      this.crew.set((await this.api.invoke(getCrewDay, { Date: date })) ?? []);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the day's crew."));
    } finally {
      this.isLoading.set(false);
    }
  }
}

function optionLabel<T>(
  options: { label: string; value: T }[],
  value: T | null | undefined,
): string {
  return options.find((o) => o.value === value)?.label ?? "";
}
