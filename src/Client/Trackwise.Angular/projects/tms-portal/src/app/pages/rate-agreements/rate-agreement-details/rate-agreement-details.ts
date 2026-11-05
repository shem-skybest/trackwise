import { DatePipe } from "@angular/common";
import { Component, computed, effect, inject, input, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, required } from "@angular/forms/signals";
import { Router, RouterLink } from "@angular/router";
import {
  Api,
  approveRateAgreement,
  getRateAgreementById,
  reviseRateAgreement,
  setRateAgreementBillingRules,
  withdrawRateAgreement,
  type RateAgreementDto,
} from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import {
  Badge,
  Card,
  EmptyState,
  Grid,
  InfoItem,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiTextField,
  UiToggleField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { dateToIso, isoToDate } from "@/shared/utils";
import { RateLinesTable } from "../components/rate-lines-table/rate-lines-table";
import { fuelMethodLabel, statusIntent, statusLabel } from "../utils/rate-agreement-labels";
import { describeTerms } from "../utils/rate-terms";

interface ReviseModel {
  name: string;
  effectiveFrom: Date | null;
}

@Component({
  selector: "app-rate-agreement-details",
  templateUrl: "./rate-agreement-details.html",
  imports: [
    Badge,
    Card,
    DatePipe,
    EmptyState,
    FormField,
    FormRoot,
    Grid,
    InfoItem,
    PageHeader,
    RateLinesTable,
    RouterLink,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiTextField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class RateAgreementDetails {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  public readonly id = input.required<string>();

  protected readonly agreement = signal<RateAgreementDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);
  protected readonly reviseOpen = signal(false);

  protected readonly canManage = this.permissions.hasPermission(Permission.Tariff.Manage);
  protected readonly isDraft = computed(() => this.agreement()?.status === "draft");
  protected readonly canRevise = computed(() =>
    ["active", "superseded"].includes(this.agreement()?.status ?? ""),
  );
  protected readonly canWithdraw = computed(() =>
    ["draft", "active"].includes(this.agreement()?.status ?? ""),
  );
  protected readonly canSetBillingRules = computed(
    () => this.canManage && !!this.agreement() && this.agreement()?.status !== "withdrawn",
  );

  protected readonly statusLabel = statusLabel;
  protected readonly statusIntent = statusIntent;
  protected readonly fuelMethodLabel = fuelMethodLabel;
  protected readonly terms = computed(() => {
    const agreement = this.agreement();
    return agreement ? describeTerms(agreement) : [];
  });
  protected readonly isoToDate = isoToDate;

  protected readonly reviseModel = signal<ReviseModel>({ name: "", effectiveFrom: null });

  protected readonly reviseForm = form(
    this.reviseModel,
    (p) => {
      required(p.name, { message: "Name the revision, e.g. HFI rates October 1, 2026." });
      maxLength(p.name, 200, { message: "Keep the name under 200 characters." });
      required(p.effectiveFrom, { message: "Pick the date the new rates start." });
    },
    {
      submission: {
        action: async () => {
          const v = this.reviseModel();
          try {
            const revision = await this.api.invoke(reviseRateAgreement, {
              id: this.id(),
              body: { name: v.name.trim(), effectiveFrom: dateToIso(v.effectiveFrom)! },
            });
            this.reviseOpen.set(false);
            this.toast.showSuccess("Revision drafted - change the rates, then approve it.");
            await this.router.navigate(["/rate-agreements", revision.id, "edit"]);
          } catch (error) {
            this.toast.showError(errorMessage(error, "Could not start the revision."));
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => void this.load(this.id()));
  }

  protected openRevise(): void {
    this.reviseModel.set({ name: "", effectiveFrom: null });
    this.reviseOpen.set(true);
  }

  protected approve(): void {
    const agreement = this.agreement();
    if (!agreement) {
      return;
    }

    this.toast.confirm({
      header: "Approve rate agreement",
      message:
        `Put "${agreement.name}" in force from ${agreement.effectiveFrom}? ` +
        "The client's current agreement closes the day before.",
      acceptLabel: "Approve",
      accept: () =>
        this.run(
          () => this.api.invoke(approveRateAgreement, { id: this.id() }),
          "Agreement approved",
        ),
    });
  }

  protected withdraw(): void {
    this.toast.confirm({
      header: "Withdraw rate agreement",
      message: "Trips already priced keep their price. New trips won't find these rates.",
      acceptLabel: "Withdraw",
      severity: "danger",
      accept: () =>
        this.run(
          () => this.api.invoke(withdrawRateAgreement, { id: this.id() }),
          "Agreement withdrawn",
        ),
    });
  }

  protected setBillingRules(change: { billExtraDrops?: boolean; ratesIncludeVat?: boolean }): void {
    const agreement = this.agreement();
    if (!agreement) {
      return;
    }

    const body = {
      billExtraDrops: change.billExtraDrops ?? agreement.billExtraDrops ?? false,
      ratesIncludeVat: change.ratesIncludeVat ?? agreement.ratesIncludeVat ?? false,
    };
    void this.run(
      () => this.api.invoke(setRateAgreementBillingRules, { id: this.id(), body }),
      "Billing rules saved - price open trips again to apply them",
    );
  }

  private async run(action: () => Promise<unknown>, success: string): Promise<void> {
    this.isBusy.set(true);
    try {
      await action();
      this.toast.showSuccess(success);
      await this.load(this.id());
    } catch (error) {
      this.toast.showError(errorMessage(error, "That didn't work."));
    } finally {
      this.isBusy.set(false);
    }
  }

  private async load(id: string): Promise<void> {
    this.isLoading.set(true);
    try {
      this.agreement.set(await this.api.invoke(getRateAgreementById, { id }));
    } catch {
      this.agreement.set(null);
    } finally {
      this.isLoading.set(false);
    }
  }
}

function errorMessage(error: unknown, fallback: string): string {
  return (
    (error as { error?: { error?: string; message?: string } })?.error?.error ??
    (error as { error?: { message?: string } })?.error?.message ??
    fallback
  );
}
