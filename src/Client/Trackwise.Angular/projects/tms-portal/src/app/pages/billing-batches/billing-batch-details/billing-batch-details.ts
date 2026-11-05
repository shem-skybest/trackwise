import { DatePipe } from "@angular/common";
import { Component, computed, effect, inject, input, signal } from "@angular/core";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  addBillingBatchTollCharges,
  Api,
  cancelBillingBatch,
  deleteBillingBatchCharge,
  forwardBillingBatch,
  getBillingBatchById,
  getBillingBatchPdf,
  lockBillingBatch,
  markBillingBatchBilled,
  refreshBillingBatch,
  removeBillingBatchTrip,
  unlockBillingBatch,
  type BillingBatchChargeDto,
  type BillingBatchDto,
  type BillingBatchTripDto,
} from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Alert,
  Badge,
  Card,
  Grid,
  InfoItem,
  Spinner,
  Stack,
  Typography,
  UiButton,
} from "@trackwise/shared/ui";
import { downloadBlobFile } from "@trackwise/shared/utils";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { isoToDate } from "@/shared/utils";
import { BatchChargesTable } from "../components/batch-charges-table/batch-charges-table";
import { BatchNoteDialog } from "../components/batch-note-dialog/batch-note-dialog";
import { BatchTripsTable } from "../components/batch-trips-table/batch-trips-table";
import { BillingChargeDialog } from "../components/billing-charge-dialog/billing-charge-dialog";
import { batchStatusIntent, batchStatusLabel } from "../utils/billing-batch-labels";

type NoteStep = "unlock" | "cancel" | "billed";

const NOTE_STEPS: Record<
  NoteStep,
  {
    header: string;
    label: string;
    hint: string;
    confirm: string;
    done: string;
    multiline: boolean;
    danger: boolean;
  }
> = {
  unlock: {
    header: "Unlock batch",
    label: "Why is it being unlocked?",
    hint: "Kept on the batch, e.g. Demurrage was missing",
    confirm: "Unlock",
    done: "Batch unlocked",
    multiline: true,
    danger: false,
  },
  cancel: {
    header: "Cancel batch",
    label: "Why is it being cancelled?",
    hint: "The trips go back to the list to bill; the reference stays used",
    confirm: "Cancel batch",
    done: "Batch cancelled",
    multiline: true,
    danger: true,
  },
  billed: {
    header: "Mark billed",
    label: "Invoice number",
    hint: "As Accounting billed it in SAS, e.g. BI4907 / BS7259",
    confirm: "Mark billed",
    done: "Marked billed",
    multiline: false,
    danger: false,
  },
};

@Component({
  selector: "app-billing-batch-details",
  templateUrl: "./billing-batch-details.html",
  imports: [
    Alert,
    Badge,
    BatchChargesTable,
    BatchNoteDialog,
    BatchTripsTable,
    BillingChargeDialog,
    Card,
    CurrencyFormatPipe,
    DatePipe,
    Grid,
    InfoItem,
    PageHeader,
    Spinner,
    Stack,
    Typography,
    UiButton,
  ],
})
export class BillingBatchDetails {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  public readonly id = input.required<string>();

  protected readonly batch = signal<BillingBatchDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);
  protected readonly chargeDialogOpen = signal(false);
  protected readonly noteStep = signal<NoteStep | null>(null);
  protected readonly noteDialogOpen = signal(false);

  protected readonly canManage = this.permissions.hasPermission(Permission.Invoice.Manage);
  protected readonly status = computed(() => this.batch()?.status);
  protected readonly isDraft = computed(() => this.status() === "draft");
  protected readonly problems = computed(() =>
    (this.batch()?.trips ?? []).filter((t) => t.problem),
  );
  protected readonly tripRates = computed(() =>
    (this.batch()?.trips ?? []).reduce((sum, t) => sum + (t.amount ?? 0), 0),
  );
  protected readonly note = computed(() => {
    const step = this.noteStep();
    return step ? NOTE_STEPS[step] : null;
  });

  protected readonly statusLabel = batchStatusLabel;
  protected readonly statusIntent = batchStatusIntent;
  protected readonly isoToDate = isoToDate;

  constructor() {
    effect(() => void this.load(this.id()));
  }

  protected refresh(): void {
    void this.run(() => this.api.invoke(refreshBillingBatch, { id: this.id() }), "Batch refreshed");
  }

  protected addTolls(): void {
    void this.run(
      () => this.api.invoke(addBillingBatchTollCharges, { id: this.id() }),
      "Tolls and parking added from the cash funds",
    );
  }

  protected lock(): void {
    this.toast.confirm({
      header: "Lock batch",
      message: "Lock the batch? Its charges are frozen until it is unlocked.",
      acceptLabel: "Lock",
      accept: () =>
        this.run(() => this.api.invoke(lockBillingBatch, { id: this.id() }), "Batch locked"),
    });
  }

  protected forward(): void {
    this.toast.confirm({
      header: "Forward to Accounting",
      message: "Record that the printed summary went to Accounting today?",
      acceptLabel: "Forward",
      accept: () =>
        this.run(
          () => this.api.invoke(forwardBillingBatch, { id: this.id(), body: {} }),
          "Forwarded to Accounting",
        ),
    });
  }

  protected askNote(step: NoteStep): void {
    this.noteStep.set(step);
    this.noteDialogOpen.set(true);
  }

  protected async onNote(text: string): Promise<void> {
    const id = this.id();
    const step = this.noteStep();
    const actions: Record<NoteStep, () => Promise<BillingBatchDto>> = {
      unlock: () => this.api.invoke(unlockBillingBatch, { id, body: { reason: text } }),
      cancel: () => this.api.invoke(cancelBillingBatch, { id, body: { reason: text } }),
      billed: () => this.api.invoke(markBillingBatchBilled, { id, body: { invoiceNumber: text } }),
    };
    if (step && (await this.run(actions[step], NOTE_STEPS[step].done))) {
      this.noteDialogOpen.set(false);
    }
  }

  protected removeTrip(trip: BillingBatchTripDto): void {
    this.toast.confirm({
      header: "Take trip off the batch",
      message: `Take trip ${trip.tripNumber} off this batch? It goes back to the list to bill, with any charges on it removed.`,
      acceptLabel: "Take off",
      accept: () =>
        this.run(
          () => this.api.invoke(removeBillingBatchTrip, { id: this.id(), tripId: trip.tripId! }),
          `Trip ${trip.tripNumber} taken off`,
        ),
    });
  }

  protected onChargeSaved(batch: BillingBatchDto): void {
    this.batch.set(batch);
  }

  protected removeCharge(charge: BillingBatchChargeDto): void {
    void this.run(
      () => this.api.invoke(deleteBillingBatchCharge, { id: this.id(), chargeId: charge.id! }),
      "Charge deleted",
    );
  }

  protected async downloadPdf(): Promise<void> {
    const batch = this.batch();
    if (!batch) {
      return;
    }

    try {
      const blob = await this.api.invoke(getBillingBatchPdf, { id: batch.id! });
      downloadBlobFile(blob, `${batch.referenceNumber}.pdf`);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Could not print the summary."));
    }
  }

  private async run(action: () => Promise<BillingBatchDto>, success: string): Promise<boolean> {
    this.isBusy.set(true);
    try {
      this.batch.set(await action());
      this.toast.showSuccess(success);
      return true;
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "That didn't work."));
      return false;
    } finally {
      this.isBusy.set(false);
    }
  }

  private async load(id: string): Promise<void> {
    this.isLoading.set(true);
    try {
      this.batch.set(await this.api.invoke(getBillingBatchById, { id }));
    } catch {
      this.batch.set(null);
    } finally {
      this.isLoading.set(false);
    }
  }
}
