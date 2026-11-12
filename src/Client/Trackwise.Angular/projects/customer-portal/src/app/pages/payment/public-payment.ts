import { DatePipe } from "@angular/common";
import { Component, computed, effect, inject, input, signal } from "@angular/core";
import { ErrorState, LoadingSkeleton, ToastService } from "@trackwise/shared";
import {
  Api,
  confirmPublicPayment,
  createPublicCheckoutSession,
  getPublicInvoice,
  type PublicInvoiceDto,
  type PublicPaymentState,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Alert,
  Badge,
  Divider,
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDataTable,
  UiNumberField,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { PublicLayout } from "@/shared/layout";

@Component({
  selector: "cp-public-payment",
  templateUrl: "./public-payment.html",
  imports: [
    Alert,
    Badge,
    CurrencyFormatPipe,
    DatePipe,
    Divider,
    ErrorState,
    Grid,
    Icon,
    LoadingSkeleton,
    PublicLayout,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiDataTable,
    UiNumberField,
  ],
})
export class PublicPayment {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);

  protected readonly tenantId = input.required<string>();
  protected readonly token = input.required<string>();

  protected readonly invoice = signal<PublicInvoiceDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isProcessing = signal(false);
  protected readonly error = signal<string | null>(null);
  /** What the gateway said when the customer came back from the hosted page (?paid=1). */
  protected readonly returnState = signal<PublicPaymentState | null>(null);
  protected readonly paymentSuccess = computed(() => {
    const state = this.returnState();
    return state === "paid" || state === "not_checked";
  });
  protected readonly paymentPending = computed(() => this.returnState() === "pending");
  protected readonly paymentAmount = signal<number | null>(null);

  /** Currency reported by the invoice - drives the currency input + formatting. */
  protected readonly currency = computed(() => this.invoice()?.total?.currency ?? "USD");

  /** True when the invoice carries any tax - drives the Rate% / Tax columns + breakdown row. */
  protected readonly hasTax = computed(() => {
    const inv = this.invoice();
    if (!inv) return false;
    if (inv.taxBehavior && inv.taxBehavior !== "exclusive") return true;
    if ((inv.taxTotal?.amount ?? 0) > 0) return true;
    return (
      inv.lineItems?.some((li) => (li.taxAmount ?? 0) > 0 || (li.taxRatePercent ?? 0) > 0) ?? false
    );
  });

  protected readonly isReverseCharge = computed(
    () => this.invoice()?.taxBehavior === "reverse_charge",
  );

  /**
   * Region-aware label for the tax column / row. The public DTO doesn't carry the
   * tenant's region, so default to "Tax"; if the breakdown carries a recognizable
   * tax_type it could be made smarter later.
   */
  protected readonly taxLabel = computed(() => "Tax");

  constructor() {
    effect(() => {
      const tenantId = this.tenantId();
      const token = this.token();
      if (tenantId && token) {
        this.load(tenantId, token);
      }
    });
  }

  /**
   * Back from the hosted payment page (the success URL carries ?paid=1): ask the API to check
   * with the gateway first, so the payment is recorded even if the gateway's callback never
   * arrives, then load the invoice with the payment on it.
   */
  private async load(tenantId: string, token: string): Promise<void> {
    if (new URLSearchParams(window.location.search).get("paid") === "1") {
      this.isLoading.set(true);
      try {
        const confirmation = await this.api.invoke(confirmPublicPayment, { tenantId, token });
        this.returnState.set(confirmation.state ?? "pending");
      } catch (err) {
        console.error("Failed to confirm payment:", err);
        this.returnState.set("pending");
      }
    }
    await this.loadInvoice(tenantId, token);
  }

  protected async checkAgain(): Promise<void> {
    await this.load(this.tenantId(), this.token());
  }

  private async loadInvoice(tenantId: string, token: string): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const invoiceData = await this.api.invoke(getPublicInvoice, { tenantId, token });
      this.invoice.set(invoiceData);
      this.paymentAmount.set(invoiceData.amountDue ?? 0);
    } catch (err: unknown) {
      console.error("Failed to load invoice:", err);
      const errorMessage =
        err instanceof Error && err.message.includes("expired")
          ? "This payment link has expired or been revoked."
          : "Unable to load invoice. The link may be invalid or expired.";
      this.error.set(errorMessage);
    } finally {
      this.isLoading.set(false);
    }
  }

  protected getStatusSeverity(): UiBadgeIntent {
    const status = this.invoice()?.status;
    switch (status) {
      case "paid":
        return "success";
      case "partially_paid":
        return "info";
      case "issued":
        return "warn";
      case "cancelled":
        return "danger";
      default:
        return "info";
    }
  }

  protected getStatusLabel(): string {
    const status = this.invoice()?.status;
    switch (status) {
      case "paid":
        return "Paid";
      case "partially_paid":
        return "Partially Paid";
      case "issued":
        return "Pending";
      case "draft":
        return "Draft";
      case "cancelled":
        return "Cancelled";
      default:
        return status ?? "Unknown";
    }
  }

  protected async processPayment(): Promise<void> {
    const amount = this.paymentAmount();
    if (!amount || amount <= 0) {
      this.toastService.showError("Please enter a valid payment amount");
      return;
    }

    this.isProcessing.set(true);
    try {
      const baseUrl = window.location.origin + window.location.pathname;
      const result = await this.api.invoke(createPublicCheckoutSession, {
        tenantId: this.tenantId(),
        token: this.token(),
        body: {
          amount,
          successUrl: `${baseUrl}?paid=1`,
          cancelUrl: baseUrl,
        },
      });

      if (!result.url) {
        this.toastService.showError("Could not start payment session.");
        return;
      }

      // Redirect to the hosted payment page (Xendit or Stripe), which returns the customer to
      // successUrl on completion or cancelUrl on cancel.
      window.location.href = result.url;
    } catch (err) {
      console.error("Payment failed:", err);
      this.toastService.showError("Payment processing failed. Please try again.");
      this.isProcessing.set(false);
    }
  }

  protected canPay(): boolean {
    const inv = this.invoice();
    return (inv?.canPay ?? false) && (inv?.amountDue ?? 0) > 0;
  }
}
