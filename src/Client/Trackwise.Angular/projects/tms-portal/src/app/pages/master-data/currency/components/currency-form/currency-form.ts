import { Component, computed, effect, input, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  maxLength,
  readonly as readonlyWhen,
  required,
  validate,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import type { CurrencyDto } from "@trackwise/shared/api";
import {
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiNumberField,
  UiTextareaField,
  UiTextField,
  UiToggleField,
  ValidatedForm,
} from "@trackwise/shared/ui";

/** What the parent receives on submit: blanks collapse to null for the API. */
export interface CurrencyFormValue {
  code: string;
  name: string;
  numericCode: string | null;
  symbol: string | null;
  minorUnits: number;
  notes: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface CurrencyFormModel {
  code: string;
  name: string;
  numericCode: string;
  symbol: string;
  minorUnits: number;
  notes: string;
  isActive: boolean;
}

const EMPTY: CurrencyFormModel = {
  code: "",
  name: "",
  numericCode: "",
  symbol: "",
  minorUnits: 2,
  notes: "",
  isActive: true,
};

/** Mirrors Currency.NormalizeDigits. */
const digits = (value: string): string => value.replace(/\D/g, "");

@Component({
  selector: "app-currency-form",
  templateUrl: "./currency-form.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    Icon,
    RouterLink,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiFormField,
    UiNumberField,
    UiTextareaField,
    UiTextField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class CurrencyForm {
  public readonly isLoading = input(false);
  public readonly currency = input<CurrencyDto | null>(null);
  public readonly submitLabel = input("Create Currency");

  /**
   * Code is immutable after creation: every Money in the schema already holds it as text, so a
   * rename would strand every amount ever denominated in it.
   */
  public readonly isEdit = input(false);

  public readonly save = output<CurrencyFormValue>();

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - every amount already carries it."
      : "ISO 4217, three letters. Never allocated for you.",
  );

  protected readonly model = signal<CurrencyFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      // The one required code in the module - a standards body assigns it, so a blank here is an
      // omission rather than a request for one.
      required(p.code, { message: "A currency needs its ISO 4217 code." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      validate(p.code, ({ value }) => {
        const entered = value().trim().toUpperCase();
        return entered.length === 0 || /^[A-Z]{3}$/.test(entered)
          ? null
          : { kind: "code", message: "An ISO 4217 code is three letters, e.g. PHP." };
      });

      validate(p.numericCode, ({ value }) => {
        const entered = digits(value().trim());
        return value().trim().length === 0 || entered.length === 3
          ? null
          : { kind: "numericCode", message: "An ISO 4217 numeric code is three digits, e.g. 608." };
      });

      validate(p.minorUnits, ({ value }) => {
        const entered = value();
        return entered >= 0 && entered <= 4
          ? null
          : {
              kind: "minorUnits",
              message: "Minor units are between 0 and 4 - 2 for most currencies, 0 for the yen.",
            };
      });

      maxLength(p.symbol, 10, { message: "Symbol must be 10 characters or fewer." });
      maxLength(p.notes, 2000, { message: "Notes must be 2000 characters or fewer." });
    },
    {
      submission: {
        action: async () => {
          this.save.emit(this.toFormValue());
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const currency = this.currency();
      if (!currency) {
        return;
      }

      this.model.set({
        code: currency.code ?? "",
        name: currency.name ?? "",
        numericCode: currency.numericCode ?? "",
        symbol: currency.symbol ?? "",
        minorUnits: currency.minorUnits ?? 2,
        notes: currency.notes ?? "",
        isActive: currency.isActive ?? true,
      });
    });
  }

  private toFormValue(): CurrencyFormValue {
    const value = this.model();

    return {
      code: value.code.trim().toUpperCase(),
      name: value.name.trim(),

      // Sent as digits so the form and the importer agree on what was entered.
      numericCode: digits(value.numericCode) || null,
      symbol: value.symbol.trim() || null,
      minorUnits: value.minorUnits,
      notes: value.notes.trim() || null,
      isActive: value.isActive,
    };
  }
}
