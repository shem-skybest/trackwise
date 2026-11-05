import { Component, effect, inject, input, output, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, required } from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { freightModeOptions, serviceLevelOptions } from "@trackwise/shared/api/enums";
import type { FreightMode, ServiceLevel } from "@trackwise/shared/api/generated";
import {
  Alert,
  Grid,
  Icon,
  Spinner,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDateField,
  UiFormField,
  UiSelectField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ContainerTypeOptionsService, ToastService } from "@/core/services";

export interface TariffFormValue {
  name: string;
  mode: FreightMode;
  serviceLevel: ServiceLevel;
  originLane?: string | null;
  destinationLane?: string | null;
  containerTypeId?: string | null;
  currency: string;
  effectiveFrom: string;
  carrierId?: string | null;
  carrier?: unknown | null;
  contractId?: string | null;
}

interface TariffFormModel {
  name: string;
  mode: FreightMode | null;
  serviceLevel: ServiceLevel | null;
  originLane: string;
  destinationLane: string;
  containerTypeId: string | null;
  currency: string;
  effectiveFrom: Date | null;
  carrier: unknown | null;
  contractId: string;
}

const EMPTY: TariffFormModel = {
  name: "",
  mode: null,
  serviceLevel: null,
  originLane: "",
  destinationLane: "",
  containerTypeId: null,
  currency: "USD",
  effectiveFrom: null,
  carrier: null,
  contractId: "",
};

@Component({
  selector: "app-tariff-form",
  templateUrl: "./tariff-form.html",
  imports: [
    Alert,
    FormField,
    FormRoot,
    Grid,
    Icon,
    RouterLink,
    Spinner,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiDateField,
    UiFormField,
    UiSelectField,
    UiTextField,
    ValidatedForm,
  ],
})
export class TariffForm {
  private readonly toastService = inject(ToastService);
  private readonly containerTypes = inject(ContainerTypeOptionsService);

  protected readonly freightModes = freightModeOptions;
  protected readonly serviceLevels = serviceLevelOptions;
  protected readonly containerTypeOptions = this.containerTypes.options;

  public readonly mode = input.required<"create" | "edit">();
  public readonly initial = input<Partial<TariffFormValue> | null>(null);
  public readonly isLoading = input(false);

  public readonly save = output<TariffFormValue>();
  public readonly remove = output<void>();

  protected readonly model = signal<TariffFormModel>({
    ...EMPTY,
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, {
        message: "Tariff name is required.",
      });

      maxLength(p.name, 200, {
        message: "Tariff name cannot exceed 200 characters.",
      });

      required(p.mode, {
        message: "Freight mode is required.",
      });

      required(p.serviceLevel, {
        message: "Service level is required.",
      });

      required(p.currency, {
        message: "Currency is required.",
      });

      maxLength(p.currency, 3, {
        message: "Currency must use a 3-letter ISO code.",
      });

      required(p.effectiveFrom, {
        message: "Effective date is required.",
      });
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
    void this.containerTypes.load();

    effect(() => {
      const initialData = this.initial();

      if (!initialData) {
        return;
      }

      if (this.mode() === "edit" && this.form().dirty()) {
        return;
      }

      void this.containerTypes.include(initialData.containerTypeId);
      this.patch(initialData);
    });
  }

  protected askRemove(): void {
    this.toastService.confirm({
      header: "Delete Tariff",
      message: "Are you sure you want to delete this tariff?",
      accept: () => this.remove.emit(),
    });
  }

  private toFormValue(): TariffFormValue {
    const value = this.model();

    return {
      name: value.name.trim(),
      mode: value.mode!,
      serviceLevel: value.serviceLevel!,
      currency: value.currency.trim().toUpperCase(),
      effectiveFrom: this.formatLocalDate(value.effectiveFrom),
      originLane: value.originLane.trim() || null,
      destinationLane: value.destinationLane.trim() || null,
      containerTypeId: value.containerTypeId,
      carrierId: this.getCarrierId(value.carrier),
      carrier: value.carrier,
      contractId: value.contractId.trim() || null,
    };
  }

  private getCarrierId(carrier: unknown): string | null {
    if (carrier && typeof carrier === "object" && "id" in carrier) {
      return String((carrier as { id?: unknown }).id ?? "") || null;
    }

    return null;
  }

  private formatLocalDate(date: Date | null): string {
    if (!date) {
      return "";
    }

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");

    return `${year}-${month}-${day}`;
  }

  private patch(src: Partial<TariffFormValue>): void {
    this.model.update((value) => ({
      ...value,

      name: src.name ?? value.name,

      mode: src.mode !== undefined ? src.mode : value.mode,

      serviceLevel: src.serviceLevel !== undefined ? src.serviceLevel : value.serviceLevel,

      originLane: src.originLane ?? value.originLane,

      destinationLane: src.destinationLane ?? value.destinationLane,

      containerTypeId:
        src.containerTypeId !== undefined ? src.containerTypeId : value.containerTypeId,

      currency: src.currency ?? value.currency,

      effectiveFrom: src.effectiveFrom ? this.parseDate(src.effectiveFrom) : value.effectiveFrom,

      carrier: src.carrier !== undefined ? src.carrier : value.carrier,

      contractId: src.contractId ?? value.contractId,
    }));
  }

  private parseDate(value: string): Date {
    const [year, month, day] = value.split("-").map(Number);

    return new Date(year, month - 1, day);
  }
}
