import { DecimalPipe } from "@angular/common";
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
import { addBookingCharge, Api, type ChargeCategory } from "@trackwise/shared/api";
import {
  UiButton,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { statusKey } from "../../utils/booking-labels";

/**
 * Adds a charge that was not in the quotation. Quoted charges are locked, so this is the only way
 * to bill something extra (detention, extra handling, a goodwill discount...).
 */
@Component({
  selector: "app-add-booking-charge-dialog",
  templateUrl: "./add-booking-charge-dialog.html",
  imports: [
    DecimalPipe,
    UiButton,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
  ],
})
export class AddBookingChargeDialog {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);

  readonly bookingId = input.required<string>();
  readonly currency = input.required<string>();
  /** Show buying cost. Hide it for roles that may add a charge but not see costs. */
  readonly showCosts = input(true);

  readonly open = model(false);
  /** Emitted after the charge is saved, so the page can reload. */
  readonly added = output<void>();

  protected readonly chargeCategoryOptions: ReadonlyArray<{
    label: string;
    value: ChargeCategory;
  }> = [
    { label: "Base", value: "base" },
    { label: "Accessorial", value: "accessorial" },
    { label: "Surcharge", value: "surcharge" },
    { label: "Tax", value: "tax" },
    { label: "Discount", value: "discount" },
  ];

  protected readonly category = signal<ChargeCategory | null>(null);
  protected readonly description = signal("");
  protected readonly quantity = signal<number | null>(1);
  protected readonly unitSelling = signal<number | null>(null);
  protected readonly unitBuying = signal<number | null>(0);
  protected readonly reason = signal("");
  protected readonly isBusy = signal(false);

  /** A discount is entered as a positive amount and applied as a reduction, like quotation lines. */
  protected readonly isDiscount = computed(() => statusKey(this.category()) === "discount");

  private readonly sign = computed(() => (this.isDiscount() ? -1 : 1));

  protected readonly sellingTotal = computed(
    () => this.sign() * (this.quantity() ?? 0) * (this.unitSelling() ?? 0),
  );
  protected readonly buyingTotal = computed(
    () => this.sign() * (this.quantity() ?? 0) * (this.unitBuying() ?? 0),
  );

  protected readonly isValid = computed(
    () =>
      this.category() !== null &&
      this.description().trim().length > 0 &&
      (this.quantity() ?? 0) > 0 &&
      (this.unitSelling() ?? -1) >= 0 &&
      (this.unitBuying() ?? -1) >= 0 &&
      this.reason().trim().length > 0,
  );

  constructor() {
    // Start every opening from a clean form.
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  protected async submit(): Promise<void> {
    const category = this.category();
    if (!category || !this.isValid() || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(addBookingCharge, {
        id: this.bookingId(),
        body: {
          category,
          description: this.description().trim(),
          quantity: this.quantity() ?? 0,
          unitSelling: this.unitSelling() ?? 0,
          unitBuying: this.unitBuying() ?? 0,
          reason: this.reason().trim(),
        },
      });

      this.toastService.showSuccess("Charge added.");
      this.open.set(false);
      this.added.emit();
    } catch {
      this.toastService.showError("Failed to add the charge. The booking may be closed.");
    } finally {
      this.isBusy.set(false);
    }
  }

  private reset(): void {
    this.category.set(null);
    this.description.set("");
    this.quantity.set(1);
    this.unitSelling.set(null);
    this.unitBuying.set(0);
    this.reason.set("");
  }
}
