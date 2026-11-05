import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import {
  disabled,
  form,
  FormField,
  FormRoot,
  max,
  min,
  pattern,
  required,
} from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  addWorkfileContainers,
  Api,
  updateWorkfileContainer,
  type WorkfileContainerDto,
} from "@trackwise/shared/api";
import {
  Grid,
  UiButton,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ContainerTypeOptionsService, ToastService } from "@/core/services";

interface SlotModel {
  containerTypeId: string | null;
  quantity: number | null;
  containerNumber: string;
  sealNumber: string;
  grossWeightKg: number | null;
  packageCount: number | null;
  packagingType: string;
  cbm: number | null;
  notes: string;
}

/**
 * Adds container slots to a workfile, or edits one. Adding books "4 x 40HC" with no numbers - the
 * usual case, since boxes get their numbers when the truck pulls them out; a number limits it to one.
 */
@Component({
  selector: "app-workfile-container-dialog",
  templateUrl: "./workfile-container-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    UiButton,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class WorkfileContainerDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly containerTypes = inject(ContainerTypeOptionsService);

  public readonly workfileId = input.required<string>();
  /** The slot being edited; null adds new slots. */
  public readonly slot = input<WorkfileContainerDto | null>(null);
  public readonly open = model(false);
  public readonly saved = output();

  protected readonly typeOptions = this.containerTypes.options;
  protected readonly isEdit = computed(() => this.slot() !== null);
  protected readonly hasNumber = computed(() => this.model().containerNumber.trim().length > 0);

  protected readonly model = signal<SlotModel>(emptyModel());

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.containerTypeId, { message: "Pick a container size." });
      required(p.quantity, { message: "Enter how many." });
      min(p.quantity, 1, { message: "Add at least one." });
      max(p.quantity, 50, { message: "Add at most 50 at a time." });
      disabled(p.quantity, { when: () => this.hasNumber() });
      pattern(p.containerNumber, /^([A-Za-z]{4}\d{7})?$/, {
        message: "4 letters and 7 digits, like MAEU7025097.",
      });
      min(p.grossWeightKg, 0, { message: "Can't be negative." });
      min(p.packageCount, 0, { message: "Can't be negative." });
      min(p.cbm, 0, { message: "Can't be negative." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const fields = {
            containerTypeId: v.containerTypeId!,
            containerNumber: v.containerNumber.trim().toUpperCase() || null,
            sealNumber: v.sealNumber.trim() || null,
            grossWeightKg: v.grossWeightKg,
            packageCount: v.packageCount,
            packagingType: v.packagingType.trim() || null,
            cbm: v.cbm,
            notes: v.notes.trim() || null,
          };

          try {
            const slot = this.slot();
            if (slot) {
              await this.api.invoke(updateWorkfileContainer, {
                id: this.workfileId(),
                containerId: slot.id!,
                body: fields,
              });
            } else {
              await this.api.invoke(addWorkfileContainers, {
                id: this.workfileId(),
                body: { ...fields, quantity: fields.containerNumber ? 1 : v.quantity! },
              });
            }

            this.toast.showSuccess(slot ? "Container updated." : "Containers added.");
            this.open.set(false);
            this.saved.emit();
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Could not save the container."));
          }
          return undefined;
        },
      },
    },
  );

  protected async onOpened(): Promise<void> {
    const slot = this.slot();
    this.model.set(slot ? fromSlot(slot) : emptyModel());
    await this.containerTypes.include(slot?.containerTypeId);
  }
}

function emptyModel(): SlotModel {
  return {
    containerTypeId: null,
    quantity: 1,
    containerNumber: "",
    sealNumber: "",
    grossWeightKg: null,
    packageCount: null,
    packagingType: "",
    cbm: null,
    notes: "",
  };
}

function fromSlot(slot: WorkfileContainerDto): SlotModel {
  return {
    containerTypeId: slot.containerTypeId ?? null,
    quantity: 1,
    containerNumber: slot.containerNumber ?? "",
    sealNumber: slot.sealNumber ?? "",
    grossWeightKg: slot.grossWeightKg ?? null,
    packageCount: slot.packageCount ?? null,
    packagingType: slot.packagingType ?? "",
    cbm: slot.cbm ?? null,
    notes: slot.notes ?? "",
  };
}
