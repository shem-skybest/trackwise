import { Component, computed, inject, signal, type WritableSignal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { Permission } from "@trackwise/shared";
import {
  Api,
  cancelWorkfile,
  clearWorkfileReferenceClosure,
  closeWorkfile,
  forwardWorkfileToAccounting,
  getGensets,
  getWorkfileById,
  recordWorkfileGensetHours,
  removeWorkfileGensetAddOn,
  requestWorkfileReferenceClosure,
  returnWorkfileToOps,
  setWorkfileGensetAddOn,
  submitWorkfileForApproval,
  type GensetBillingType,
  type WorkfileDto,
  type WorkfileStatus,
} from "@trackwise/shared/api";
import {
  billIndicatorOptions,
  creditTypeOptions,
  gensetBillingTypeOptions,
  reportCategoryOptions,
  truckingVariantOptions,
  workfileStatusOptions,
  workfileTypeOptions,
} from "@trackwise/shared/api/enums";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Grid,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { DataContainer, PageHeader } from "@/shared/components";
import { WorkfileContainers } from "../components/workfile-containers/workfile-containers";
import { WorkfileLegLoads } from "../components/workfile-leg-loads/workfile-leg-loads";
import { WorkfileLegs } from "../components/workfile-legs/workfile-legs";
import { WorkfileShipmentDialog } from "../components/workfile-shipment-dialog/workfile-shipment-dialog";
import { WorkfileTimeline } from "../components/workfile-timeline/workfile-timeline";

const STATUS_INTENT: Record<string, UiBadgeIntent> = {
  open: "info",
  manager_review: "warn",
  accounting_review: "warn",
  closed: "success",
  cancelled: "danger",
};

function labelFrom(options: { label: string; value: string }[], value: string | null | undefined) {
  return options.find((o) => o.value === value)?.label ?? null;
}

@Component({
  selector: "app-workfile-details",
  templateUrl: "./workfile-details.html",
  imports: [
    Badge,
    Card,
    DataContainer,
    DateFormatPipe,
    Grid,
    PageHeader,
    RouterLink,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    WorkfileContainers,
    WorkfileLegLoads,
    WorkfileLegs,
    WorkfileShipmentDialog,
    WorkfileTimeline,
  ],
})
export class WorkfileDetailsComponent {
  private readonly api = inject(Api);
  private readonly route = inject(ActivatedRoute);
  private readonly toastService = inject(ToastService);
  private readonly permissionService = inject(PermissionService);

  // Follows the route rather than its snapshot: a workfile links to its truck legs and a leg back
  // to its parent, which reuses this component with a new id.
  private id: string | null = null;

  protected readonly workfile = signal<WorkfileDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);

  protected readonly cancelDialogVisible = signal(false);
  protected readonly cancelReason = signal("");

  protected readonly returnDialogVisible = signal(false);
  protected readonly returnReason = signal("");

  protected readonly closeDialogVisible = signal(false);
  protected readonly closeRemarks = signal("");

  protected readonly clearClosureDialogVisible = signal(false);
  protected readonly clearClosureRemarks = signal("");

  protected readonly gensetOptions = signal<{ label: string; value: string }[]>([]);
  protected readonly gensetAddOnDialogVisible = signal(false);
  protected readonly gensetSelectedId = signal<string | null>(null);
  protected readonly gensetBillingType = signal<GensetBillingType>("itemized");
  protected readonly gensetHourlyRate = signal<number | null>(null);
  protected readonly gensetFlatAmount = signal<number | null>(null);
  protected readonly gensetNotes = signal("");

  protected readonly recordHoursDialogVisible = signal(false);
  protected readonly recordHoursValue = signal<number | null>(null);

  protected readonly removeGensetDialogVisible = signal(false);

  protected readonly gensetBillingTypeOptions = gensetBillingTypeOptions;
  protected readonly unassignedGensetValue = "";

  protected readonly gensetSelectOptions = computed(() => [
    { label: "Not yet assigned", value: this.unassignedGensetValue },
    ...this.gensetOptions(),
  ]);

  protected readonly typeLabel = computed(() =>
    labelFrom(workfileTypeOptions, this.workfile()?.type),
  );
  protected readonly statusLabel = computed(
    () => labelFrom(workfileStatusOptions, this.workfile()?.status) ?? "",
  );
  protected readonly variantLabel = computed(() =>
    labelFrom(truckingVariantOptions, this.workfile()?.variant),
  );
  protected readonly reportCategoryLabel = computed(() =>
    labelFrom(reportCategoryOptions, this.workfile()?.reportCategory),
  );
  protected readonly billIndicatorLabel = computed(() =>
    labelFrom(billIndicatorOptions, this.workfile()?.billIndicator),
  );
  protected readonly creditTypeLabel = computed(() =>
    labelFrom(creditTypeOptions, this.workfile()?.creditType),
  );
  protected readonly gensetBillingTypeLabel = computed(() =>
    labelFrom(gensetBillingTypeOptions, this.workfile()?.gensetBillingType),
  );

  protected readonly shipmentDialogVisible = signal(false);

  protected readonly canManage = computed(() =>
    this.permissionService.hasPermission(Permission.Workfile.Manage),
  );

  protected readonly canManageGenset = computed(
    () =>
      this.workfile()?.isEditable === true &&
      this.permissionService.hasPermission(Permission.Workfile.Manage),
  );

  protected readonly canRecordGensetHours = computed(
    () => this.canManageGenset() && this.workfile()?.gensetBillingType === "itemized",
  );

  // Every action needs three things to line up: the status machine allows the transition, the file
  // is not frozen by a closure request, and the user holds the verb for that stage. The API
  // re-checks all three - these only decide whether the button is worth showing.
  private readonly unlocked = computed(() => {
    const workfile = this.workfile();
    return !!workfile && !workfile.isReferenceLocked;
  });

  protected readonly canSubmit = computed(
    () =>
      this.unlocked() &&
      this.workfile()?.status === "open" &&
      this.permissionService.hasPermission(Permission.Workfile.Manage),
  );

  protected readonly canReturnToOps = computed(
    () =>
      this.unlocked() &&
      this.workfile()?.status === "manager_review" &&
      this.permissionService.hasPermission(Permission.Workfile.Approve),
  );

  protected readonly canForward = computed(
    () =>
      this.unlocked() &&
      this.workfile()?.status === "manager_review" &&
      this.permissionService.hasPermission(Permission.Workfile.ForwardBilling),
  );

  /**
   * Closing deliberately ignores the lock: a closure request freezes the file so that Accounting
   * can finish it, so this is the one move a locked file still accepts.
   */
  protected readonly canClose = computed(
    () =>
      this.workfile()?.status === "accounting_review" &&
      this.permissionService.hasPermission(Permission.Workfile.Approve),
  );

  protected readonly canRequestClosure = computed(() => {
    const status = this.workfile()?.status;
    return (
      this.unlocked() &&
      status !== "closed" &&
      status !== "cancelled" &&
      this.permissionService.hasPermission(Permission.Workfile.Manage)
    );
  });

  protected readonly canClearClosure = computed(
    () =>
      this.workfile()?.isReferenceLocked === true &&
      this.permissionService.hasPermission(Permission.Workfile.ClearClosure),
  );

  protected readonly canCancel = computed(() => {
    const status = this.workfile()?.status;
    return (
      this.unlocked() &&
      (status === "open" || status === "manager_review") &&
      this.permissionService.hasPermission(Permission.Workfile.Manage)
    );
  });

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.id = params.get("id");
      void this.load();
    });
  }

  protected statusIntent(status: WorkfileStatus | undefined): UiBadgeIntent {
    return (status && STATUS_INTENT[status]) || "secondary";
  }

  protected reload(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    if (!this.id) {
      return;
    }

    this.isLoading.set(true);
    try {
      this.workfile.set(await this.api.invoke(getWorkfileById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected openCancelDialog(): void {
    this.cancelReason.set("");
    this.cancelDialogVisible.set(true);
  }

  protected async confirmCancel(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(cancelWorkfile, {
        id: this.id,
        body: { reason: this.cancelReason().trim() },
      });

      this.cancelDialogVisible.set(false);
      this.toastService.showSuccess("Workfile cancelled");
      await this.load();
    } catch {
      this.toastService.showError("Failed to cancel this workfile.");
    } finally {
      this.isBusy.set(false);
    }
  }

  protected submitForApproval(): Promise<void> {
    return this.run(
      (id) => this.api.invoke(submitWorkfileForApproval, { id }),
      "Workfile submitted for approval",
      "Failed to submit this workfile for approval.",
    );
  }

  protected forwardToAccounting(): Promise<void> {
    return this.run(
      (id) => this.api.invoke(forwardWorkfileToAccounting, { id }),
      "Workfile forwarded to Accounting",
      "Failed to forward this workfile to Accounting.",
    );
  }

  protected requestClosure(): Promise<void> {
    return this.run(
      (id) => this.api.invoke(requestWorkfileReferenceClosure, { id }),
      "Closure requested - the file stays locked until Accounting clears it",
      "Failed to request closure of this reference.",
    );
  }

  protected openReturnDialog(): void {
    this.returnReason.set("");
    this.returnDialogVisible.set(true);
  }

  protected confirmReturn(): Promise<void> {
    return this.run(
      (id) =>
        this.api.invoke(returnWorkfileToOps, {
          id,
          body: { reason: this.returnReason().trim() },
        }),
      "Workfile returned to ops",
      "Failed to return this workfile to ops.",
      this.returnDialogVisible,
    );
  }

  protected openCloseDialog(): void {
    this.closeRemarks.set("");
    this.closeDialogVisible.set(true);
  }

  protected confirmClose(): Promise<void> {
    return this.run(
      (id) =>
        this.api.invoke(closeWorkfile, {
          id,
          body: { remarks: this.closeRemarks().trim() || undefined },
        }),
      "Workfile closed",
      "Failed to close this workfile.",
      this.closeDialogVisible,
    );
  }

  protected openClearClosureDialog(): void {
    this.clearClosureRemarks.set("");
    this.clearClosureDialogVisible.set(true);
  }

  protected confirmClearClosure(): Promise<void> {
    return this.run(
      (id) =>
        this.api.invoke(clearWorkfileReferenceClosure, {
          id,
          body: { remarks: this.clearClosureRemarks().trim() || undefined },
        }),
      "Closure cleared - the file is editable again",
      "Failed to clear the closure on this workfile.",
      this.clearClosureDialogVisible,
    );
  }

  protected async openGensetDialog(): Promise<void> {
    const file = this.workfile();
    if (!file) {
      return;
    }

    this.gensetSelectedId.set(file.gensetId ?? this.unassignedGensetValue);
    this.gensetBillingType.set(file.gensetBillingType ?? "itemized");
    this.gensetHourlyRate.set(file.gensetHourlyRate ?? null);
    this.gensetFlatAmount.set(file.gensetFlatAmount ?? null);
    this.gensetNotes.set(file.gensetNotes ?? "");
    this.gensetAddOnDialogVisible.set(true);

    if (this.gensetOptions().length === 0) {
      const result = await this.api.invoke(getGensets, { IsActive: true, PageSize: 100 });
      this.gensetOptions.set(
        (result.items ?? []).map((g) => ({ label: g.name ?? g.code ?? "", value: g.id ?? "" })),
      );
    }
  }

  protected confirmGensetAddOn(): Promise<void> {
    return this.run(
      (id) =>
        this.api.invoke(setWorkfileGensetAddOn, {
          id,
          body: {
            gensetId: this.gensetSelectedId() || null,
            billingType: this.gensetBillingType(),
            hourlyRate: this.gensetBillingType() === "itemized" ? this.gensetHourlyRate() : null,
            flatAmount:
              this.gensetBillingType() === "profit_share" ? this.gensetFlatAmount() : null,
            notes: this.gensetNotes().trim() || null,
          },
        }),
      "Genset add-on saved",
      "Failed to save the genset add-on.",
      this.gensetAddOnDialogVisible,
    );
  }

  protected openRecordHoursDialog(): void {
    this.recordHoursValue.set(this.workfile()?.gensetHoursUsed ?? null);
    this.recordHoursDialogVisible.set(true);
  }

  protected confirmRecordHours(): Promise<void> {
    const hoursUsed = this.recordHoursValue();
    if (hoursUsed === null) {
      return Promise.resolve();
    }

    return this.run(
      (id) => this.api.invoke(recordWorkfileGensetHours, { id, body: { hoursUsed } }),
      "Genset hours recorded",
      "Failed to record genset hours.",
      this.recordHoursDialogVisible,
    );
  }

  protected confirmRemoveGensetAddOn(): Promise<void> {
    return this.run(
      (id) => this.api.invoke(removeWorkfileGensetAddOn, { id }),
      "Genset add-on removed",
      "Failed to remove the genset add-on.",
      this.removeGensetDialogVisible,
    );
  }

  /**
   * Every lifecycle move has the same shape: call, then reload so the new status and the buttons
   * that go with it come from the server instead of from a guess here.
   */
  private async run(
    action: (id: string) => Promise<unknown>,
    success: string,
    failure: string,
    dialog?: WritableSignal<boolean>,
  ): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await action(this.id);
      dialog?.set(false);
      this.toastService.showSuccess(success);
      await this.load();
    } catch {
      this.toastService.showError(failure);
    } finally {
      this.isBusy.set(false);
    }
  }
}
