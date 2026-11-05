import { DatePipe } from "@angular/common";
import { Component, inject, input, model, output, signal } from "@angular/core";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  getMaintenanceRecords,
  setIncidentRepairs,
  type IncidentDto,
  type MaintenanceRecordDto,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiCheckboxField,
  UiDialog,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";

/** How far either side of the incident a repair on the same truck is offered. */
const DAYS_BEFORE = 3;
const DAYS_AFTER = 60;

/**
 * Picks the repairs the incident caused from the truck's maintenance records around its date, e.g.
 * the windshield replaced four days after an accident. Saves the whole set.
 */
@Component({
  selector: "app-incident-repairs-dialog",
  templateUrl: "./incident-repairs-dialog.html",
  imports: [
    CurrencyFormatPipe,
    DatePipe,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiCheckboxField,
    UiDialog,
  ],
})
export class IncidentRepairsDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly incident = input.required<IncidentDto>();
  public readonly open = model(false);
  public readonly saved = output<IncidentDto>();

  protected readonly records = signal<MaintenanceRecordDto[]>([]);
  protected readonly selected = signal<ReadonlySet<string>>(new Set());
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);

  protected async onOpened(): Promise<void> {
    const incident = this.incident();
    this.selected.set(new Set((incident.repairs ?? []).map((r) => r.id!)));
    if (!incident.truckId) {
      this.records.set([]);
      return;
    }

    const at = new Date(incident.occurredAt!);
    const from = new Date(at.getTime() - DAYS_BEFORE * 86_400_000);
    const to = new Date(at.getTime() + DAYS_AFTER * 86_400_000);
    this.isLoading.set(true);
    try {
      const page = await this.api.invoke(getMaintenanceRecords, {
        TruckId: incident.truckId,
        FromDate: from.toISOString(),
        ToDate: to.toISOString(),
        OrderBy: "ServiceDate",
        PageSize: 100,
      });
      this.records.set(page.items ?? []);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the truck's repairs."));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected isSelected(id: string | undefined): boolean {
    return !!id && this.selected().has(id);
  }

  protected toggle(id: string | undefined, on: boolean): void {
    if (!id) return;
    const next = new Set(this.selected());
    if (on) next.add(id);
    else next.delete(id);
    this.selected.set(next);
  }

  protected async save(): Promise<void> {
    this.isSaving.set(true);
    try {
      const incident = await this.api.invoke(setIncidentRepairs, {
        id: this.incident().id!,
        body: { repairIds: [...this.selected()] },
      });
      this.toast.showSuccess("Repairs linked");
      this.open.set(false);
      this.saved.emit(incident);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't link the repairs."));
    } finally {
      this.isSaving.set(false);
    }
  }
}
