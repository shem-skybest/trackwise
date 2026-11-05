import { Component, inject, input, model, output, signal } from "@angular/core";
import { Api, setBookingReferenceNumber, type BookingDto } from "@trackwise/shared/api";
import {
  Stack,
  Typography,
  UiButton,
  UiDialog,
  UiFormField,
  UiTextField,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";

/**
 * Types in the BKN-RFV number nSFS gave a Cold Chain booking, while nSFS still issues them. nSFS
 * often numbers a booking only after the truck has loaded, so this works at any stage.
 */
@Component({
  selector: "app-booking-number-dialog",
  templateUrl: "./booking-number-dialog.html",
  imports: [Stack, Typography, UiButton, UiDialog, UiFormField, UiTextField],
})
export class BookingNumberDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly booking = input.required<BookingDto>();
  public readonly open = model(false);
  public readonly saved = output<string>();

  protected readonly number = signal("");
  protected readonly isBusy = signal(false);

  protected onOpened(): void {
    this.number.set(this.booking().bookingNumber ?? "");
  }

  protected async save(): Promise<void> {
    const typed = this.number().trim();
    if (!typed || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      const reference = await this.api.invoke(setBookingReferenceNumber, {
        id: this.booking().id!,
        body: { referenceNumber: typed },
      });
      this.toast.showSuccess(`Saved as ${reference}.`);
      this.open.set(false);
      this.saved.emit(reference);
    } catch {
      // The API error interceptor has already shown why (e.g. another booking has the number).
    } finally {
      this.isBusy.set(false);
    }
  }
}
