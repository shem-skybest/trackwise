import { Component, effect, inject, input, signal } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  getRateAgreementById,
  updateRateAgreement,
  type RateAgreementDto,
} from "@trackwise/shared/api";
import { EmptyState, Spinner, Stack } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import {
  RateAgreementForm,
  type RateAgreementFormValue,
} from "../components/rate-agreement-form/rate-agreement-form";

@Component({
  selector: "app-rate-agreement-edit",
  templateUrl: "./rate-agreement-edit.html",
  imports: [EmptyState, PageHeader, RateAgreementForm, Spinner, Stack],
})
export class RateAgreementEdit {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  public readonly id = input.required<string>();

  protected readonly agreement = signal<RateAgreementDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);

  constructor() {
    effect(() => void this.load(this.id()));
  }

  protected async update(value: RateAgreementFormValue): Promise<void> {
    this.isSaving.set(true);
    try {
      // The customer is fixed once created; the update command has no customer, so the server
      // ignores the one the form value carries.
      await this.api.invoke(updateRateAgreement, {
        id: this.id(),
        body: { ...value, id: this.id() },
      });
      this.toast.showSuccess("Draft saved");
      await this.router.navigate(["/rate-agreements", this.id()]);
    } catch (error) {
      this.toast.showError(
        (error as { error?: { error?: string } })?.error?.error ?? "Could not save the agreement.",
      );
    } finally {
      this.isSaving.set(false);
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
