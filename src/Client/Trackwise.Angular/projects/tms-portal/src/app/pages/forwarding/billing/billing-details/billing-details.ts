import { CurrencyPipe, DatePipe, DecimalPipe } from "@angular/common";
import { Component, computed, inject, signal, type OnInit } from "@angular/core";
import { ActivatedRoute, RouterLink } from "@angular/router";
import {
  Api,
  approveBookingInvoice,
  cancelBookingInvoice,
  getInvoiceById,
  issueBookingInvoice,
  PageHeader,
  Permission,
  PermissionService,
  rejectBookingInvoice,
  reopenBookingInvoice,
  sendBookingInvoice,
  submitBookingInvoice,
  ToastService,
  type InvoiceDto,
} from "@trackwise/shared";
import {
  Alert,
  Spinner,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDialog,
  UiFormField,
  UiTextareaField,
  UiTextField,
} from "@trackwise/shared/ui";

@Component({
  selector: "app-billing-details",
  templateUrl: "billing-details.html",
  imports: [
    Alert,
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    PageHeader,
    RouterLink,
    Spinner,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiDialog,
    UiFormField,
    UiTextareaField,
    UiTextField,
  ],
})
export class BillingDetailsComponent implements OnInit {
  private readonly api = inject(Api);
  private readonly route = inject(ActivatedRoute);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly billing = signal<InvoiceDto | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isBusy = signal(false);

  // Dialogs
  protected readonly issueDialogVisible = signal(false);
  protected readonly rejectDialogVisible = signal(false);
  protected readonly rejectReason = signal("");
  protected readonly sendDialogVisible = signal(false);
  protected readonly sendEmail = signal("");
  protected readonly cancelDialogVisible = signal(false);
  protected readonly cancelReason = signal("");

  // ── Permissions (the server enforces these either way) ─────────────────────
  protected readonly canManage = this.permissions.hasPermission(Permission.Invoice.Manage);
  // Swap for a dedicated approve permission once one exists.
  protected readonly canApprove = this.permissions.hasPermission(Permission.Invoice.Approve);

  // ── Status flags ───────────────────────────────────────────────────────────
  private readonly status = computed(() => this.billing()?.status);
  protected readonly isDraft = computed(() => this.status() === "draft");
  protected readonly isPendingApproval = computed(() => this.status() === "pending_approval");
  protected readonly isApproved = computed(() => this.status() === "approved");
  protected readonly isRejected = computed(() => this.status() === "rejected");
  protected readonly isCancelled = computed(() => this.status() === "cancelled");
  protected readonly canSend = computed(
    () => this.status() === "issued" || this.status() === "sent",
  );
  protected readonly hasSent = computed(() => this.status() === "sent");

  /** Approved and issued invoices must be reversed with a credit note, never cancelled in place. */
  protected readonly canCancel = computed(() => {
    const b = this.billing();
    const cancellable = ["draft", "pending_approval", "rejected"];
    return (
      !!b && this.canManage && cancellable.includes(b.status ?? "") && !(b.payments?.length ?? 0)
    );
  });

  /** True when there is any primary action, so the toolbar divider only shows when needed. */
  protected readonly hasPrimaryAction = computed(
    () =>
      (this.isDraft() && this.canManage) ||
      (this.isPendingApproval() && this.canApprove) ||
      (this.isRejected() && this.canManage) ||
      (this.isApproved() && this.canManage) ||
      (this.canSend() && this.canManage),
  );

  // ── Display ────────────────────────────────────────────────────────────────
  protected readonly pageTitle = computed(
    () => `${"Invoice " + (this.billing()?.number ?? "Billing Details")}`,
  );

  protected readonly subtitle = computed(() => {
    const b = this.billing();
    if (!b) return "";
    const parts = [this.label(b.status)];
    if (b.bookingReference) parts.push(`from booking (${b.bookingReference})`);
    return parts.join(" · ");
  });

  protected readonly currency = computed(() => this.billing()?.total?.currency ?? "USD");

  protected readonly sortedLineItems = computed(() =>
    [...(this.billing()?.lineItems ?? [])].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
  );

  protected readonly totalPaid = computed(() =>
    (this.billing()?.payments ?? []).reduce((sum, p) => sum + (p.amount?.amount ?? 0), 0),
  );

  protected readonly balanceDue = computed(
    () => (this.billing()?.total?.amount ?? 0) - this.totalPaid(),
  );

  protected readonly isOverdue = computed(() => {
    const b = this.billing();
    if (!b?.dueDate || b.status === "paid" || b.status === "cancelled") return false;
    return new Date(b.dueDate) < new Date() && this.balanceDue() > 0;
  });

  protected readonly statusClass = computed(() => {
    switch (this.billing()?.status) {
      case "paid":
        return "bg-emerald-100 text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300";
      case "overdue":
      case "cancelled":
      case "rejected":
        return "bg-red-100 text-red-800 dark:bg-red-950/40 dark:text-red-300";
      case "pending_approval":
      case "partially_paid":
        return "bg-amber-100 text-amber-800 dark:bg-amber-950/40 dark:text-amber-300";
      case "approved":
      case "issued":
      case "sent":
        return "bg-blue-100 text-blue-800 dark:bg-blue-950/40 dark:text-blue-300";
      default:
        return "bg-muted text-muted-foreground";
    }
  });

  protected label(value?: string | null): string {
    return value
      ? value
          .replace(/_/g, " ")
          .replace(/([a-z])([A-Z])/g, "$1 $2")
          .replace(/\b\w/g, (c) => c.toUpperCase())
      : "—";
  }

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(): Promise<void> {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) return;

    this.isLoading.set(true);
    try {
      this.billing.set(await this.api.invoke(getInvoiceById, { id }));
    } catch {
      this.toastService.showError("Failed to load billing details.");
    } finally {
      this.isLoading.set(false);
    }
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  /**
   * Runs one transition. The endpoints return the updated invoice, so on success the page
   * state is replaced directly. On failure the status may have changed under us, so reload.
   */
  private async run(
    action: (id: string) => Promise<InvoiceDto>,
    success: string,
    failure: string,
  ): Promise<boolean> {
    const id = this.billing()?.id;
    if (!id || this.isBusy()) return false;

    this.isBusy.set(true);
    try {
      this.billing.set(await action(id));
      this.toastService.showSuccess(success);
      return true;
    } catch {
      this.toastService.showError(failure);
      await this.load();
      return false;
    } finally {
      this.isBusy.set(false);
    }
  }

  protected submit(): Promise<boolean> {
    return this.run(
      (id) => this.api.invoke(submitBookingInvoice, { id }),
      "Invoice submitted for approval.",
      "Failed to submit the invoice.",
    );
  }

  protected approve(): Promise<boolean> {
    return this.run(
      (id) => this.api.invoke(approveBookingInvoice, { id }),
      "Invoice approved.",
      "Failed to approve the invoice.",
    );
  }

  protected reopen(): Promise<boolean> {
    return this.run(
      (id) => this.api.invoke(reopenBookingInvoice, { id }),
      "Invoice moved back to draft.",
      "Failed to reopen the invoice.",
    );
  }

  protected async confirmIssue(): Promise<void> {
    const ok = await this.run(
      (id) => this.api.invoke(issueBookingInvoice, { id }),
      "Invoice issued.",
      "Failed to issue the invoice.",
    );
    if (ok) this.issueDialogVisible.set(false);
  }

  protected openRejectDialog(): void {
    this.rejectReason.set("");
    this.rejectDialogVisible.set(true);
  }

  protected async confirmReject(): Promise<void> {
    const reason = this.rejectReason().trim();
    if (!reason) return;
    const ok = await this.run(
      (id) => this.api.invoke(rejectBookingInvoice, { id, body: { reason } }),
      "Invoice rejected.",
      "Failed to reject the invoice.",
    );
    if (ok) this.rejectDialogVisible.set(false);
  }

  protected openSendDialog(): void {
    this.sendEmail.set(this.billing()?.sentToEmail ?? "");
    this.sendDialogVisible.set(true);
  }

  protected async confirmSend(): Promise<void> {
    const email = this.sendEmail().trim();
    if (!email) return;
    const ok = await this.run(
      (id) => this.api.invoke(sendBookingInvoice, { id, body: { email } }),
      "Invoice sent.",
      "Failed to send the invoice.",
    );
    if (ok) this.sendDialogVisible.set(false);
  }

  protected openCancelDialog(): void {
    this.cancelReason.set("");
    this.cancelDialogVisible.set(true);
  }

  protected async confirmCancel(): Promise<void> {
    const reason = this.cancelReason().trim();
    if (!reason) return;
    const ok = await this.run(
      (id) => this.api.invoke(cancelBookingInvoice, { id, body: { reason } }),
      "Invoice cancelled.",
      "Failed to cancel the invoice.",
    );
    if (ok) this.cancelDialogVisible.set(false);
  }
}
