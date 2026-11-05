import { Component, effect, inject, input, model, output, signal, untracked } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  hidden,
  maxLength,
  required,
  validate,
} from "@angular/forms/signals";
import { ErrorCodes } from "@trackwise/shared";
import {
  Api,
  createLocation,
  type Address,
  type GeoPoint,
  type LocationDto,
  type LocationType,
} from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import {
  Alert,
  Stack,
  UiButton,
  UiCheckboxField,
  UiDateField,
  UiDialog,
  UiFormField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { dateToTimeOfDay } from "@/shared/utils";

interface SaveSiteModel {
  name: string;
  cutoff: Date | null;
  confirmedDistinct: boolean;
  distinctReason: string;
}

const EMPTY: SaveSiteModel = {
  name: "",
  cutoff: null,
  confirmedDistinct: false,
  distinctReason: "",
};

/**
 * Turns an address typed on a load into a saved delivery location for that load's customer, so
 * the next load to the same outlet is one pick instead of a map search.
 */
@Component({
  selector: "app-save-site-dialog",
  templateUrl: "./save-site-dialog.html",
  imports: [
    Alert,
    FormField,
    FormRoot,
    Stack,
    UiButton,
    UiCheckboxField,
    UiDateField,
    UiDialog,
    UiFormField,
    UiTextField,
    ValidatedForm,
  ],
})
export class SaveSiteDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly open = model(false);
  public readonly customerId = input<string | null>(null);
  public readonly customerName = input<string | null>(null);
  public readonly address = input<Address | null>(null);
  public readonly location = input<GeoPoint | null>(null);
  public readonly saved = output<LocationDto>();

  /** Set when the server's duplicate probe blocked the save; the steward can then override it. */
  protected readonly duplicateMessage = signal<string | null>(null);

  protected readonly model = signal<SaveSiteModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Give the outlet a name." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      hidden(p.confirmedDistinct, { when: () => !this.duplicateMessage() });
      hidden(p.distinctReason, {
        when: ({ valueOf }) => !this.duplicateMessage() || !valueOf(p.confirmedDistinct),
      });
      validate(p.distinctReason, ({ value }) =>
        value().trim()
          ? null
          : { kind: "reason", message: "Say why this is not the existing location." },
      );
    },
    {
      submission: {
        action: async () => {
          await this.submit();
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      if (!this.open()) {
        return;
      }
      // Suggest the first line of the address - usually the place name Google returned.
      const street = untracked(this.address)?.streetAddress ?? "";
      this.model.set({ ...EMPTY, name: street.split(",")[0].trim() });
      this.duplicateMessage.set(null);
    });
  }

  private async submit(): Promise<void> {
    const v = this.model();
    const location = this.location();
    const hasPin = !!location && !(location.latitude === 0 && location.longitude === 0);

    try {
      const created = await this.api.invoke(createLocation, {
        body: {
          name: v.name.trim(),
          type: "delivery_point" as LocationType,
          customerId: this.customerId(),
          address: this.address() ?? undefined,
          latitude: hasPin ? location!.latitude : null,
          longitude: hasPin ? location!.longitude : null,
          defaultCutoffTime: dateToTimeOfDay(v.cutoff),
          confirmedDistinct: v.confirmedDistinct,
          distinctReason: v.confirmedDistinct ? v.distinctReason.trim() : null,
        },
      });

      this.toast.showSuccess(`Saved ${created.name} as a delivery location`);
      this.saved.emit(created);
      this.open.set(false);
    } catch (error) {
      const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

      if (body?.errorCode === ErrorCodes.DuplicateMasterDataRecord) {
        this.duplicateMessage.set(body.message ?? "A similar location already exists.");
        return;
      }

      this.toast.showError(body?.message ?? "Could not save the delivery location.");
    }
  }
}
