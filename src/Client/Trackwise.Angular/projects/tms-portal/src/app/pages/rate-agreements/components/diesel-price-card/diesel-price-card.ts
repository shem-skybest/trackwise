import { CurrencyPipe, DatePipe } from "@angular/common";
import { Component, computed, inject, input, signal, type OnInit } from "@angular/core";
import { form, FormField, FormRoot, max, maxLength, min, required } from "@angular/forms/signals";
import { Api, getDieselPrices, setDieselPrice, type DieselPriceDto } from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import {
  Card,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { dateToIso, isoToDate } from "@/shared/utils";

interface DieselPriceModel {
  effectiveFrom: Date | null;
  pricePerLitre: number | null;
  notes: string;
}

/**
 * The diesel price "gas on top" surcharges are read against. Every agreement's diesel tier comes
 * from this one number, so it lives beside the list rather than inside any single agreement.
 */
@Component({
  selector: "app-diesel-price-card",
  templateUrl: "./diesel-price-card.html",
  imports: [
    Card,
    CurrencyPipe,
    DatePipe,
    FormField,
    FormRoot,
    Stack,
    Typography,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiTextField,
    ValidatedForm,
  ],
})
export class DieselPriceCard implements OnInit {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly canManage = input(false);

  protected readonly prices = signal<DieselPriceDto[]>([]);
  protected readonly dialogOpen = signal(false);

  /** The entry in force today: the latest one that has started. */
  protected readonly current = computed(() => {
    const today = dateToIso(new Date())!;
    return this.prices().find((p) => (p.effectiveFrom ?? "") <= today) ?? null;
  });

  protected readonly model = signal<DieselPriceModel>({
    effectiveFrom: null,
    pricePerLitre: null,
    notes: "",
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.effectiveFrom, { message: "Pick the date it takes effect." });
      required(p.pricePerLitre, { message: "Enter the price per litre." });
      min(p.pricePerLitre, 1, { message: "Enter the price per litre." });
      max(p.pricePerLitre, 1000, { message: "That's not a diesel price per litre." });
      maxLength(p.notes, 500, { message: "Keep the note under 500 characters." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          try {
            await this.api.invoke(setDieselPrice, {
              body: {
                effectiveFrom: dateToIso(v.effectiveFrom)!,
                pricePerLitre: v.pricePerLitre!,
                notes: v.notes.trim() || null,
              },
            });
            this.toast.showSuccess("Diesel price saved");
            this.dialogOpen.set(false);
            await this.load();
          } catch {
            this.toast.showError("Could not save the diesel price.");
          }
          return undefined;
        },
      },
    },
  );

  ngOnInit(): void {
    void this.load();
  }

  protected openDialog(): void {
    this.model.set({
      effectiveFrom: new Date(),
      pricePerLitre: this.current()?.pricePerLitre ?? null,
      notes: "",
    });
    this.dialogOpen.set(true);
  }

  protected toDate(value: string | undefined): Date | null {
    return isoToDate(value);
  }

  private async load(): Promise<void> {
    try {
      const result = await this.api.invoke(getDieselPrices, { PageSize: 6 });
      this.prices.set(result.items ?? []);
    } catch {
      this.prices.set([]);
    }
  }
}
