import { DatePipe, DecimalPipe, TitleCasePipe } from "@angular/common";
import { Component, inject, signal, type OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  acceptQuotation,
  addQuotationLine,
  Api,
  approveQuotation,
  convertQuotationToBooking,
  expireQuotation,
  generateQuotationPdf,
  getQuotationById,
  issueQuotation,
  rejectQuotation,
  removeQuotationLine,
  submitQuotation,
  type ConvertQuotationToBookingRequest,
  type QuotationDto,
  type QuotationLineDto,
  type QuotationStatus,
} from "@trackwise/shared/api";
import {
  Alert,
  ConfirmDeleteDialog,
  Grid,
  Icon,
  Spinner,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
} from "@trackwise/shared/ui";
import { ContainerTypeOptionsService, ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import {
  PdfViewerComponent,
  type PdfLoader,
} from "@/shared/components/pdf-viewer/pdf-viewer.component";
import {
  QuotationLineForm,
  type QuotationLineFormValue,
} from "../components/quotation-line-form/quotation-line-form";
import { QuotationLinesTable } from "../components/quotation-lines-table/quotation-lines-table";

@Component({
  selector: "app-quotation-details",
  templateUrl: "./quotation-details.html",
  imports: [
    Alert,
    DatePipe,
    DecimalPipe,
    Grid,
    Icon,
    PageHeader,
    QuotationLineForm,
    QuotationLinesTable,
    Spinner,
    Stack,
    Surface,
    TitleCasePipe,
    Typography,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    ConfirmDeleteDialog,
    PdfViewerComponent,
  ],
})
export class QuotationDetailsComponent implements OnInit {
  private readonly api = inject(Api);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly containerTypes = inject(ContainerTypeOptionsService);

  protected readonly containerTypeOptions = this.containerTypes.options;

  protected readonly quotation = signal<QuotationDto | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isBusy = signal(false);

  protected readonly addLineDialogVisible = signal(false);
  protected readonly rejectDialogVisible = signal(false);
  protected readonly rejectionReason = signal("");

  protected readonly issueDialogVisible = signal(false);
  protected readonly validityDays = signal<number>(30);

  protected readonly convertDialogVisible = signal(false);
  protected readonly convertContainerTypeId = signal<string | null>(null);
  protected readonly convertPickupDate = signal<Date | null>(null);
  protected readonly convertDeliveryDate = signal<Date | null>(null);
  protected readonly convertNotes = signal("");

  protected readonly openQuotationPdfViewer = signal(false);

  // Deletion dialog state
  protected readonly deleteLineDialogVisible = signal(false);
  protected readonly isDeletingLine = signal(false);
  protected readonly lineToDelete = signal<QuotationLineDto | null>(null);

  async ngOnInit(): Promise<void> {
    void this.containerTypes.load();
    await this.load();
  }

  /**
   * Triggered when the trash icon is clicked in the table.
   * Finds the line to display its name in the confirmation modal.
   */
  protected promptDeleteLine(lineId: string): void {
    const line = this.quotation()?.lines?.find((l) => l.id === lineId) ?? null;
    if (!line) return;

    this.lineToDelete.set(line);
    this.deleteLineDialogVisible.set(true);
  }

  /**
   * Triggered after the user types the exact entity name and confirms.
   */
  protected async onConfirmDeleteLine(): Promise<void> {
    const quotationId = this.quotation()?.id;
    const lineId = this.lineToDelete()?.id;

    if (!quotationId || !lineId) return;

    this.isDeletingLine.set(true);
    try {
      await this.api.invoke(removeQuotationLine, {
        id: quotationId,
        lineId: lineId,
      });

      this.deleteLineDialogVisible.set(false);
      this.lineToDelete.set(null);
      await this.load();
    } finally {
      this.isDeletingLine.set(false);
    }
  }

  private async load(): Promise<void> {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) return;

    this.isLoading.set(true);
    try {
      const result = await this.api.invoke(getQuotationById, { id });
      this.quotation.set(result);

      console.log(this.quotation());
    } catch {
      this.toastService.showError("Failed to load quotation.");
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async addLine(value: QuotationLineFormValue): Promise<void> {
    const quotationId = this.quotation()?.id;
    if (!quotationId || this.isBusy()) return;

    this.isBusy.set(true);
    try {
      await this.api.invoke(addQuotationLine, { id: quotationId, body: value });
      this.addLineDialogVisible.set(false);
      this.toastService.showSuccess("Line added to quotation.");
      await this.load();
    } catch {
      this.toastService.showError("Failed to add line. Check the tariff rate and currency.");
    } finally {
      this.isBusy.set(false);
    }
  }

  protected submit(): void {
    const id = this.quotation()?.id;
    if (!id) return;

    this.toastService.confirm({
      header: "Submit Quotation",
      message: "Submit this quotation for internal approval?",
      accept: async () => {
        this.isBusy.set(true);
        try {
          await this.api.invoke(submitQuotation, { id });
          this.toastService.showSuccess("Quotation submitted for approval.");
          await this.load();
        } catch {
          this.toastService.showError("Failed to submit quotation.");
        } finally {
          this.isBusy.set(false);
        }
      },
    });
  }

  protected approve(): void {
    const quotation = this.quotation();
    if (!quotation?.id) return;

    this.toastService.confirm({
      header: "Approve Quotation",
      message: `Approve this quotation for ${quotation.customerName}?`,
      accept: async () => {
        this.isBusy.set(true);
        try {
          await this.api.invoke(approveQuotation, { id: quotation.id! });
          this.toastService.showSuccess("Quotation approved.");
          await this.load();
        } catch {
          this.toastService.showError("Failed to approve quotation.");
        } finally {
          this.isBusy.set(false);
        }
      },
    });
  }

  protected openIssueDialog(): void {
    this.validityDays.set(30);
    this.issueDialogVisible.set(true);
  }

  protected async confirmIssue(): Promise<void> {
    const id = this.quotation()?.id;
    if (!id || this.isBusy()) return;

    this.isBusy.set(true);
    try {
      await this.api.invoke(issueQuotation, {
        id,
        body: { validityDays: this.validityDays() },
      });
      this.issueDialogVisible.set(false);
      this.toastService.showSuccess("Quotation issued successfully.");
      await this.load();
    } catch {
      this.toastService.showError("Failed to issue quotation.");
    } finally {
      this.isBusy.set(false);
    }
  }

  protected accept(): void {
    const id = this.quotation()?.id;
    if (!id) return;

    this.toastService.confirm({
      header: "Accept Quotation",
      message: "Mark this quotation as accepted by the customer?",
      accept: async () => {
        this.isBusy.set(true);
        try {
          await this.api.invoke(acceptQuotation, { id });
          this.toastService.showSuccess("Quotation accepted.");
          await this.load();
        } catch {
          this.toastService.showError("Failed to accept quotation.");
        } finally {
          this.isBusy.set(false);
        }
      },
    });
  }

  protected expire(): void {
    const id = this.quotation()?.id;
    if (!id) return;

    this.toastService.confirm({
      header: "Expire Quotation",
      message: "Are you sure you want to expire this quotation?",
      accept: async () => {
        this.isBusy.set(true);
        try {
          await this.api.invoke(expireQuotation, { id });
          this.toastService.showSuccess("Quotation marked as expired.");
          await this.load();
        } catch {
          this.toastService.showError("Failed to expire quotation.");
        } finally {
          this.isBusy.set(false);
        }
      },
    });
  }

  protected edit(): void {
    const id = this.quotation()?.id;
    if (!id) return;

    void this.router.navigate(["/quotations", id, "edit"]);
  }

  protected openRejectDialog(): void {
    this.rejectionReason.set("");
    this.rejectDialogVisible.set(true);
  }

  protected async confirmReject(): Promise<void> {
    const quotationId = this.quotation()?.id;
    const reason = this.rejectionReason().trim();
    if (!quotationId || !reason || this.isBusy()) return;

    this.isBusy.set(true);
    try {
      await this.api.invoke(rejectQuotation, { id: quotationId, body: { reason } });
      this.rejectDialogVisible.set(false);
      this.toastService.showSuccess("Quotation rejected.");
      await this.load();
    } catch {
      this.toastService.showError("Failed to reject quotation.");
    } finally {
      this.isBusy.set(false);
    }
  }

  protected openConvertDialog(): void {
    this.convertContainerTypeId.set(null);
    this.convertPickupDate.set(null);
    this.convertDeliveryDate.set(null);
    this.convertNotes.set("");
    this.convertDialogVisible.set(true);
  }

  protected async confirmConvertToBooking(): Promise<void> {
    const quotationId = this.quotation()?.id;
    if (!quotationId || this.isBusy()) return;

    const body: ConvertQuotationToBookingRequest = {
      containerTypeId: this.convertContainerTypeId() ?? undefined,
      requestedPickupDate: this.convertPickupDate()?.toISOString() ?? null,
      requestedDeliveryDate: this.convertDeliveryDate()?.toISOString() ?? null,
      notes: this.convertNotes().trim() || null,
    };

    this.isBusy.set(true);
    try {
      const bookingId = await this.api.invoke(convertQuotationToBooking, { id: quotationId, body });
      this.convertDialogVisible.set(false);
      this.toastService.showSuccess("Booking created from this quotation.");
      await this.router.navigateByUrl(`/forwarding/bookings-executions/${bookingId}`);
    } catch {
      this.toastService.showError("Failed to convert quotation to booking.");
    } finally {
      this.isBusy.set(false);
    }
  }

  protected statusBadgeClass(status?: QuotationStatus): string {
    switch (status) {
      case "accepted":
      case "approved":
        return "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400";
      case "issued":
        return "bg-blue-500/10 text-blue-600 dark:text-blue-400";
      case "pending_approval":
        return "bg-amber-500/10 text-amber-600 dark:text-amber-400";
      case "rejected":
      case "auto_declined":
      case "expired":
        return "bg-destructive/10 text-destructive";
      case "draft":
      default:
        return "bg-muted text-muted-foreground";
    }
  }

  async addLines(lines: QuotationLineFormValue[]): Promise<void> {
    const quotationId = this.quotation()?.id; // or this.quotation()?.id;

    // 1. Guard check narrows quotationId from `string | undefined` to `string`
    if (!quotationId) {
      console.error("Quotation ID is required to add lines.");
      return;
    }

    // 2. quotationId is now strictly `string`
    await Promise.all(
      lines.map((line) =>
        this.api.invoke(addQuotationLine, {
          id: quotationId,
          body: { ...line },
        }),
      ),
    );

    this.addLineDialogVisible.set(false);
    await this.load();
  }

  protected formatLabel(text?: string | null): string {
    if (!text) return "-";
    return text.replace(/_/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());
  }

  protected readonly loadQuotationPdf: PdfLoader = (id) =>
    this.api.invoke(generateQuotationPdf, { id });
}
