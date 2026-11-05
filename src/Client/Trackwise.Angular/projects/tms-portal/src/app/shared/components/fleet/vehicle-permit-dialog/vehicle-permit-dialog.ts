import {
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  maxLength,
  min,
  required,
  validate,
} from "@angular/forms/signals";
import {
  Api,
  createVehiclePermit,
  updateVehiclePermit,
  type VehiclePermitDto,
  type VehiclePermitType,
} from "@trackwise/shared/api";
import { vehiclePermitTypeOptions } from "@trackwise/shared/api/enums";
import { LocalizationService, ToastService } from "@trackwise/shared/services";
import {
  CurrencyField,
  Stack,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { dateToIso, isoToDate } from "@/shared/utils";

interface PermitModel {
  type: VehiclePermitType;
  number: string;
  issuer: string;
  issuedOn: Date | null;
  expiresOn: Date | null;
  cost: number | null;
  notes: string;
}

const EMPTY: PermitModel = {
  type: "lto_registration",
  number: "",
  issuer: "",
  issuedOn: null,
  expiresOn: null,
  cost: null,
  notes: "",
};

/**
 * Adds or corrects one paper on a truck or a trailer. A renewal is a new paper, not an edit of
 * the old one, so the history of past registrations stays.
 */
@Component({
  selector: "app-vehicle-permit-dialog",
  templateUrl: "./vehicle-permit-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Stack,
    UiButton,
    CurrencyField,
    UiDateField,
    UiDialog,
    UiFormField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class VehiclePermitDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly localization = inject(LocalizationService);

  public readonly visible = model<boolean>(false);
  public readonly truckId = input<string | null>(null);
  public readonly trailerId = input<string | null>(null);
  /** The paper to correct, or null to record a new one. */
  public readonly permit = input<VehiclePermitDto | null>(null);
  public readonly saved = output<void>();

  protected readonly typeOptions = vehiclePermitTypeOptions;
  protected readonly currencySymbol = this.localization.getCurrencySymbol();
  protected readonly title = computed(() => (this.permit() ? "Edit paper" : "Add paper"));
  protected readonly model = signal<PermitModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.type, { message: "Pick what kind of paper this is." });
      required(p.expiresOn, { message: "The expiry date is required." });
      maxLength(p.number, 64, { message: "The number cannot exceed 64 characters." });
      maxLength(p.issuer, 100, { message: "The issuer cannot exceed 100 characters." });
      maxLength(p.notes, 500, { message: "Notes cannot exceed 500 characters." });
      min(p.cost, 0, { message: "The cost cannot be negative." });
      validate(p.issuedOn, ({ value, valueOf }) => {
        const issued = value();
        const expires = valueOf(p.expiresOn);
        return issued && expires && issued > expires
          ? {
              kind: "issuedAfterExpiry",
              message: "The issue date must be on or before the expiry date.",
            }
          : null;
      });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const body = {
            type: v.type,
            number: v.number || null,
            issuer: v.issuer || null,
            issuedOn: dateToIso(v.issuedOn),
            expiresOn: dateToIso(v.expiresOn)!,
            cost: v.cost,
            notes: v.notes || null,
          };

          try {
            const existing = this.permit();
            if (existing?.id) {
              await this.api.invoke(updateVehiclePermit, { id: existing.id, body });
              this.toast.showSuccess("Paper updated");
            } else {
              await this.api.invoke(createVehiclePermit, {
                body: { ...body, truckId: this.truckId(), trailerId: this.trailerId() },
              });
              this.toast.showSuccess("Paper added");
            }
            this.saved.emit();
            this.visible.set(false);
          } catch {
            this.toast.showError("Failed to save the paper.");
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const permit = this.permit();
      if (this.visible()) {
        // Untracked, so typing in the form doesn't re-run this and wipe what was entered.
        untracked(() => this.form().reset(permit ? this.toModel(permit) : { ...EMPTY }));
      }
    });
  }

  protected close(): void {
    this.visible.set(false);
  }

  private toModel(permit: VehiclePermitDto): PermitModel {
    return {
      type: permit.type ?? "lto_registration",
      number: permit.number ?? "",
      issuer: permit.issuer ?? "",
      issuedOn: isoToDate(permit.issuedOn),
      expiresOn: isoToDate(permit.expiresOn),
      cost: permit.cost ?? null,
      notes: permit.notes ?? "",
    };
  }
}
