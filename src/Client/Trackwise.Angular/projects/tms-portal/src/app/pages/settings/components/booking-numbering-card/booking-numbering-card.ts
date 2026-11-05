import { Component, computed, inject, signal } from "@angular/core";
import { form, FormField, FormRoot, max, min, required } from "@angular/forms/signals";
import { Permission } from "@trackwise/shared";
import {
  Api,
  getBookingNumbering,
  setBookingNumbersTyped,
  setNextBookingNumber,
  type BookingNumberingDto,
} from "@trackwise/shared/api";
import {
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiNumberField,
  UiToggleField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { TenantService } from "@/core/services/tenant.service";

/**
 * Where the Cold Chain booking numbers (BKN-RFV) come from and stand this year. While nSFS still
 * issues them, staff type each booking's number in; at cut-over the Owner switches that off and
 * sets the next number so TrackWise carries on from nSFS. The API refuses a number already issued,
 * so it can only move forward - which is why saving asks first.
 */
@Component({
  selector: "app-booking-numbering-card",
  templateUrl: "./booking-numbering-card.html",
  imports: [
    FormField,
    FormRoot,
    Icon,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiFormField,
    UiNumberField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class BookingNumberingCard {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly tenantService = inject(TenantService);
  private readonly permissionService = inject(PermissionService);

  protected readonly canManage = computed(() =>
    this.permissionService.hasPermission(Permission.Tenant.Manage),
  );
  protected readonly numbering = signal<BookingNumberingDto | null>(null);
  protected readonly typed = signal(false);
  protected readonly savingTyped = signal(false);
  protected readonly model = signal<{ nextNumber: number | null }>({ nextNumber: null });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.nextNumber, { message: "Enter the next booking number." });
      // Not the counter's next number: the field would snap a lower entry up to it unseen. The API
      // refuses a number already issued and says which is the lowest allowed.
      min(p.nextNumber, 1, { message: "Booking numbers start at 1." });
      max(p.nextNumber, 999999, { message: "That's more than six digits." });
    },
    {
      submission: {
        action: async () => {
          const nextNumber = this.model().nextNumber!;
          this.toast.confirm({
            message:
              `The next Cold Chain booking will be number ${nextNumber} this year. ` +
              "Numbers can't be lowered again afterwards. Continue?",
            accept: () => void this.save(nextNumber),
          });
          return undefined;
        },
      },
    },
  );

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      const numbering = await this.api.invoke(getBookingNumbering);
      this.numbering.set(numbering);
      this.typed.set(numbering.typed ?? false);
      this.form().reset({ nextNumber: numbering.nextNumber ?? null });
    } catch {
      // The API error interceptor has already shown why.
    }
  }

  protected async onTypedChange(typed: boolean): Promise<void> {
    this.savingTyped.set(true);
    try {
      const numbering = await this.api.invoke(setBookingNumbersTyped, { body: { typed } });
      this.numbering.set(numbering);
      // The booking form reads the switch from the tenant's settings.
      this.tenantService.refetchTenantData();
      this.toast.showSuccess(
        typed
          ? "Booking numbers are now typed in from nSFS."
          : `TrackWise issues the booking numbers now, starting with ${numbering.nextReference}.`,
      );
    } catch {
      this.typed.set(!typed);
    } finally {
      this.savingTyped.set(false);
    }
  }

  private async save(nextNumber: number): Promise<void> {
    try {
      const numbering = await this.api.invoke(setNextBookingNumber, { body: { nextNumber } });
      this.numbering.set(numbering);
      this.form().reset({ nextNumber: numbering.nextNumber ?? null });
      this.toast.showSuccess(`The next booking will be ${numbering.nextReference}.`);
    } catch {
      // The API error interceptor has already shown why (e.g. a number already issued).
    }
  }
}
