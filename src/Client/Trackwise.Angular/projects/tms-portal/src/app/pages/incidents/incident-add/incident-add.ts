import { Component, inject, signal } from "@angular/core";
import { form, FormRoot } from "@angular/forms/signals";
import { ActivatedRoute, Router } from "@angular/router";
import { getApiErrorMessage } from "@trackwise/shared";
import { Api, createIncident, getTripById } from "@trackwise/shared/api";
import { Card, Spinner, UiButton, ValidatedForm } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { IncidentForm } from "../components/incident-form/incident-form";
import {
  emptyIncident,
  incidentCommand,
  incidentFromTrip,
  incidentSchema,
  type IncidentModel,
} from "../utils/incident-model";

/**
 * Reports an incident. Opened from a trip's crew report (`?tripId=`), it starts with the trip's
 * truck, crew and client filled in.
 */
@Component({
  selector: "app-incident-add",
  templateUrl: "./incident-add.html",
  imports: [Card, FormRoot, IncidentForm, PageHeader, Spinner, UiButton, ValidatedForm],
})
export class IncidentAdd {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly tripId = inject(ActivatedRoute).snapshot.queryParamMap.get("tripId");

  protected readonly isLoading = signal(!!this.tripId);
  protected readonly model = signal<IncidentModel>(emptyIncident());

  protected readonly form = form(this.model, incidentSchema, {
    submission: {
      action: async () => {
        try {
          const created = await this.api.invoke(createIncident, {
            body: incidentCommand(this.model()),
          });
          this.toast.showSuccess(`${created.caseNumber} recorded`);
          void this.router.navigate(["/incidents", created.id]);
        } catch (error) {
          this.toast.showError(getApiErrorMessage(error, "Couldn't record the incident."));
        }
        return undefined;
      },
    },
  });

  constructor() {
    if (this.tripId) {
      void this.prefillFromTrip(this.tripId);
    }
  }

  protected cancel(): void {
    if (this.tripId) {
      void this.router.navigate(["/trips", this.tripId, "report"]);
    } else {
      void this.router.navigate(["/incidents"]);
    }
  }

  private async prefillFromTrip(tripId: string): Promise<void> {
    try {
      const trip = await this.api.invoke(getTripById, { tripId });
      this.form().reset(incidentFromTrip(trip));
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the trip."));
    } finally {
      this.isLoading.set(false);
    }
  }
}
