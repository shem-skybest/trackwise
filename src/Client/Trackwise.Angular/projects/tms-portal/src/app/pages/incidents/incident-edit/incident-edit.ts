import { Component, inject, signal } from "@angular/core";
import { form, FormRoot } from "@angular/forms/signals";
import { ActivatedRoute, Router } from "@angular/router";
import { getApiErrorMessage } from "@trackwise/shared";
import { Api, getIncidentById, updateIncident } from "@trackwise/shared/api";
import { Card, Spinner, UiButton, ValidatedForm } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { IncidentForm } from "../components/incident-form/incident-form";
import {
  emptyIncident,
  incidentCommand,
  incidentFromDto,
  incidentSchema,
  type IncidentModel,
} from "../utils/incident-model";

/** Rewrites an incident report until it is resolved. */
@Component({
  selector: "app-incident-edit",
  templateUrl: "./incident-edit.html",
  imports: [Card, FormRoot, IncidentForm, PageHeader, Spinner, UiButton, ValidatedForm],
})
export class IncidentEdit {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  protected readonly id = inject(ActivatedRoute).snapshot.paramMap.get("id")!;

  protected readonly isLoading = signal(true);
  protected readonly caseNumber = signal<string | null>(null);
  protected readonly model = signal<IncidentModel>(emptyIncident());

  protected readonly form = form(this.model, incidentSchema, {
    submission: {
      action: async () => {
        try {
          await this.api.invoke(updateIncident, {
            id: this.id,
            body: incidentCommand(this.model()),
          });
          this.toast.showSuccess("Incident updated");
          void this.router.navigate(["/incidents", this.id]);
        } catch (error) {
          this.toast.showError(getApiErrorMessage(error, "Couldn't save the incident."));
        }
        return undefined;
      },
    },
  });

  constructor() {
    void this.load();
  }

  protected cancel(): void {
    void this.router.navigate(["/incidents", this.id]);
  }

  private async load(): Promise<void> {
    try {
      const incident = await this.api.invoke(getIncidentById, { id: this.id });
      this.caseNumber.set(incident.caseNumber ?? null);
      this.form().reset(incidentFromDto(incident));
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the incident."));
    } finally {
      this.isLoading.set(false);
    }
  }
}
