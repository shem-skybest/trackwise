import { DatePipe } from "@angular/common";
import { Component, computed, inject, input, output, signal } from "@angular/core";
import type { TripDto } from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import { Grid, Surface, Typography, UiButton } from "@trackwise/shared/ui";
import { crewDay, isHeaderEditable } from "../../utils/trip-header";
import { ReportHeaderEditor } from "../report-header-editor/report-header-editor";

/**
 * The top of the crew report, as the crew's message opens: dates, plate and crew, client and
 * commodity, drops. The truck and crew can be changed here until the first arrival is recorded.
 */
@Component({
  selector: "app-report-header",
  templateUrl: "./report-header.html",
  imports: [DatePipe, Grid, ReportHeaderEditor, Surface, Typography, UiButton],
})
export class ReportHeader {
  private readonly toast = inject(ToastService);

  public readonly trip = input.required<TripDto>();
  public readonly drops = input.required<number>();
  /** The report below has typed times not saved yet - a change here reloads the page. */
  public readonly unsaved = input(false);
  public readonly changed = output<void>();

  protected readonly editing = signal(false);
  protected readonly editable = computed(() => isHeaderEditable(this.trip()));
  protected readonly crewDay = computed(() => crewDay(this.trip()));

  protected readonly summary = computed(() => {
    const trip = this.trip();
    const loads = trip.loads ?? [];
    const distinct = (values: (string | null | undefined)[]) =>
      [...new Set(values.filter((v): v is string => !!v))].join(", ");
    const dates = (values: (string | null | undefined)[]) =>
      values.filter((v): v is string => !!v).sort();
    return {
      preLoad: dates(loads.map((l) => l.requestedPickupDate))[0] ?? null,
      rdd: dates(loads.map((l) => l.requestedDeliveryDate)).at(-1) ?? null,
      helpers: distinct([trip.helperName, trip.secondHelperName]),
      clients: distinct(loads.map((l) => l.customer?.name)),
      commodity: distinct(loads.map((l) => l.commodity)),
    };
  });

  protected edit(): void {
    if (this.unsaved()) {
      this.toast.showInfo("Save the report first - changing the trip reloads the page.");
      return;
    }
    this.editing.set(true);
  }

  protected onSaved(): void {
    this.editing.set(false);
    this.changed.emit();
  }
}
