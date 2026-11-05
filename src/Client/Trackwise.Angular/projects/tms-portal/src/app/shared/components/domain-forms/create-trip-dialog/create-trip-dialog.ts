import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  createTripFromBooking,
  type TruckDto,
  type TruckTonnageClass,
} from "@trackwise/shared/api";
import { truckTonnageClassOptions } from "@trackwise/shared/api/enums";
import { Alert, Stack, Typography, UiButton, UiDialog, UiFormField } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { SearchTruck } from "@/shared/components/search";

/**
 * Makes the trip for a confirmed Cold Chain booking: one load per drop from the pickup warehouse,
 * crewed from the day's crew plan, then priced from the client's agreement. Emits the new trip's
 * id; the page decides whether to open it or stay put.
 */
@Component({
  selector: "app-create-trip-dialog",
  templateUrl: "./create-trip-dialog.html",
  imports: [Alert, SearchTruck, Stack, Typography, UiButton, UiDialog, UiFormField],
})
export class CreateTripDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly bookingId = input.required<string>();
  public readonly bookedSize = input<TruckTonnageClass | null | undefined>(null);
  public readonly dropCount = input(0);
  public readonly open = model(false);
  public readonly created = output<string>();

  protected readonly truck = signal<TruckDto | null>(null);
  protected readonly isBusy = signal(false);

  protected readonly bookedSizeLabel = computed(() => sizeLabel(this.bookedSize()));

  /** A different size is allowed - a 4T can cover a 2T job - and is billed at the booked size. */
  protected readonly sizeMismatch = computed(() => {
    const truckSize = this.truck()?.tonnageClass;
    const booked = this.bookedSize();
    return !!truckSize && !!booked && truckSize !== booked;
  });

  protected readonly truckSizeLabel = computed(() => sizeLabel(this.truck()?.tonnageClass));

  protected onOpened(): void {
    this.truck.set(null);
  }

  protected async create(): Promise<void> {
    if (this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      const result = await this.api.invoke(createTripFromBooking, {
        id: this.bookingId(),
        body: { truckId: this.truck()?.id ?? null },
      });

      if (result.pricingError) {
        this.toast.showWarning(`Trip made, but not priced yet: ${result.pricingError}`);
      } else {
        this.toast.showSuccess("Trip made and priced from the agreement.");
      }

      this.open.set(false);
      this.created.emit(result.tripId!);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Could not make the trip."));
    } finally {
      this.isBusy.set(false);
    }
  }
}

function sizeLabel(size: string | null | undefined): string {
  return truckTonnageClassOptions.find((o) => o.value === size)?.label ?? "";
}
