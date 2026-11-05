import { Component, computed, input } from "@angular/core";
import { FormField, type FieldTree } from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { DRIVING_ROLES } from "@trackwise/shared";
import {
  accidentTypeOptions,
  incidentKindOptions,
  incidentSeverityOptions,
} from "@trackwise/shared/api/enums";
import {
  Grid,
  Stack,
  Typography,
  UiButton,
  UiCheckboxField,
  UiDateField,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
} from "@trackwise/shared/ui";
import {
  SearchCustomer,
  SearchEmployee,
  SearchHelper,
  SearchTruck,
} from "@/shared/components/search";
import type { IncidentModel } from "../../utils/incident-model";

/**
 * The incident report's fields, shared by the add and edit pages: what happened, who and what was
 * involved, the accident's injuries and damage, the police or barangay report, and the money.
 * Accident-only sections show for an accident.
 */
@Component({
  selector: "app-incident-form",
  templateUrl: "./incident-form.html",
  imports: [
    FormField,
    Grid,
    RouterLink,
    SearchCustomer,
    SearchEmployee,
    SearchHelper,
    SearchTruck,
    Stack,
    Typography,
    UiButton,
    UiCheckboxField,
    UiDateField,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
  ],
})
export class IncidentForm {
  public readonly field = input.required<FieldTree<IncidentModel>>();

  protected readonly kindOptions = incidentKindOptions;
  protected readonly severityOptions = incidentSeverityOptions;
  protected readonly accidentTypeOptions = accidentTypeOptions;
  protected readonly drivingRoles = DRIVING_ROLES;

  protected readonly isAccident = computed(() => this.field().kind().value() === "accident");
  protected readonly tripId = computed(() => this.field().tripId().value());
  protected readonly tripNumber = computed(() => this.field().tripNumber().value());

  protected clearTrip(): void {
    this.field().tripId().value.set(null);
    this.field().tripNumber().value.set(null);
  }
}
