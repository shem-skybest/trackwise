import { Component, inject, input, signal } from "@angular/core";
import { form, FormField, FormRoot, required } from "@angular/forms/signals";
import { Router, RouterLink } from "@angular/router";
import { Api, supersedeTariff, type SupersedeTariffCommand } from "@trackwise/shared/api";
import {
  Card,
  Container,
  Icon,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiFormField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";

interface SupersedeFormModel {
  effectiveFrom: Date | null;
  reason: string;
}

@Component({
  selector: "app-tariff-supersede",
  templateUrl: "./tariff-supersede.html",
  imports: [
    Card,
    Container,
    FormField,
    FormRoot,
    Icon,
    PageHeader,
    RouterLink,
    Stack,
    Typography,
    UiButton,
    UiDateField,
    UiFormField,
    UiTextareaField,
    ValidatedForm,
  ],
})
export class TariffSupersede {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  public readonly id = input.required<string>();
  protected readonly model = signal<SupersedeFormModel>({
    effectiveFrom: null,
    reason: "",
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.effectiveFrom, { message: "Effective date is required." });
    },
    {
      submission: {
        action: async () => {
          const value = this.model();
          const command: SupersedeTariffCommand = {
            effectiveFrom: this.formatDate(value.effectiveFrom),
            reason: value.reason.trim() || null,
          };

          try {
            const replacementId = await this.api.invoke(supersedeTariff, {
              id: this.id(),
              body: command,
            });
            this.toastService.showSuccess("Tariff superseded with a new version");
            await this.router.navigate(["/tariffs", replacementId]);
          } catch {
            this.toastService.showError("Failed to supersede tariff");
          }
          return undefined;
        },
      },
    },
  );

  private formatDate(date: Date | null): string {
    if (!date) return "";
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
