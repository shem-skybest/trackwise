import { Component, inject, input, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, minLength, required } from "@angular/forms/signals";
import { Router, RouterLink } from "@angular/router";
import {
  Api,
  createChargeCode,
  type ChargeCategory,
  type CreateChargeCodeCommand,
  type FreightMode,
} from "@trackwise/shared/api";
import { freightModeOptions } from "@trackwise/shared/api/enums";
import {
  Card,
  Container,
  Grid,
  Icon,
  Stack,
  Typography,
  UiButton,
  UiCheckboxField,
  UiFormField,
  UiMultiSelectField,
  UiSelectField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";

interface ChargeCodeFormModel {
  code: string;
  name: string;
  description: string;
  category: ChargeCategory | null;
  applicableModes: FreightMode[];
  defaultUnit: string;
  requiresApproval: boolean;
  taxCode: string;
}

const EMPTY: ChargeCodeFormModel = {
  code: "",
  name: "",
  description: "",
  category: "base",
  applicableModes: ["domestic"],
  defaultUnit: "per trip",
  requiresApproval: false,
  taxCode: "none",
};

@Component({
  selector: "app-tariff-charge-code-add",
  templateUrl: "./tariff-charge-code-add.html",
  imports: [
    Card,
    Container,
    FormField,
    FormRoot,
    Grid,
    Icon,
    PageHeader,
    RouterLink,
    Stack,
    Typography,
    UiButton,
    UiCheckboxField,
    UiFormField,
    UiMultiSelectField,
    UiSelectField,
    UiTextField,
    ValidatedForm,
  ],
})
export class TariffChargeCodeAdd {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  public readonly tariffId = input.required<string>();

  protected readonly backLink = () => `/tariffs/${this.tariffId()}/rates/add`;

  protected readonly modeOptions = freightModeOptions;
  protected readonly categoryOptions = [
    { label: "Base", value: "base" as ChargeCategory },
    { label: "Accessorial", value: "accessorial" as ChargeCategory },
    { label: "Surcharge", value: "surcharge" as ChargeCategory },
    { label: "Tax", value: "tax" as ChargeCategory },
    { label: "Discount", value: "discount" as ChargeCategory },
  ];
  protected readonly model = signal<ChargeCodeFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.code, { message: "Charge code is required." });
      minLength(p.code, 2, { message: "Charge code must be at least 2 characters." });
      maxLength(p.code, 20, { message: "Charge code cannot exceed 20 characters." });
      required(p.name, { message: "Charge code name is required." });
      required(p.category, { message: "Category is required." });
      required(p.applicableModes, { message: "Select at least one applicable mode." });
      required(p.defaultUnit, { message: "Default unit is required." });
    },
    {
      submission: {
        action: async () => {
          const value = this.model();
          const command: CreateChargeCodeCommand = {
            code: value.code.trim().toUpperCase(),
            name: value.name.trim(),
            description: value.description.trim() || null,
            category: value.category!,
            applicableModes: value.applicableModes,
            defaultUnit: value.defaultUnit.trim(),
            requiresApproval: value.requiresApproval,
            taxCode: value.taxCode.trim() || null,
          };

          try {
            await this.api.invoke(createChargeCode, { body: command });
            this.toastService.showSuccess(`${command.code} charge code created`);
            await this.router.navigate(["/tariffs", this.tariffId(), "rates", "add"]);
          } catch {
            this.toastService.showError("Failed to create charge code");
          }
          return undefined;
        },
      },
    },
  );
}
