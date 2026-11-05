import { Component, input } from "@angular/core";
import { FormField, type FieldTree } from "@angular/forms/signals";
import {
  Grid,
  Typography,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiToggleField,
} from "@trackwise/shared/ui";
import {
  EXTRA_DROP_SIZES,
  extraDropSizeLabel,
  TOLLS_OPTIONS,
  type RateTermsModel,
} from "../../utils/rate-terms";

/**
 * The notes under a quotation's rate table: drops the rate covers, what further drops, foul trips
 * and backloads cost, demurrage, and pass-on charges. Blank means the quotation doesn't say.
 */
@Component({
  selector: "app-rate-terms-fields",
  templateUrl: "./rate-terms-fields.html",
  imports: [FormField, Grid, Typography, UiFormField, UiNumberField, UiSelectField, UiToggleField],
})
export class RateTermsFields {
  public readonly field = input.required<FieldTree<RateTermsModel>>();

  protected readonly sizes = EXTRA_DROP_SIZES;
  protected readonly sizeLabel = extraDropSizeLabel;
  protected readonly tollsOptions = TOLLS_OPTIONS;
}
