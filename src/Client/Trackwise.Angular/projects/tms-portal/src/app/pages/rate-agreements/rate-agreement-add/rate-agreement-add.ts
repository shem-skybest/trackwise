import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createRateAgreement } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import {
  RateAgreementForm,
  type RateAgreementFormValue,
} from "../components/rate-agreement-form/rate-agreement-form";

@Component({
  selector: "app-rate-agreement-add",
  templateUrl: "./rate-agreement-add.html",
  imports: [PageHeader, RateAgreementForm],
})
export class RateAgreementAdd {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly isSaving = signal(false);

  protected async create(value: RateAgreementFormValue): Promise<void> {
    this.isSaving.set(true);
    try {
      const created = await this.api.invoke(createRateAgreement, { body: value });
      this.toast.showSuccess("Draft saved - approve it to put the rates in force.");
      await this.router.navigate(["/rate-agreements", created.id]);
    } catch (error) {
      this.toast.showError(
        (error as { error?: { error?: string } })?.error?.error ?? "Could not save the agreement.",
      );
    } finally {
      this.isSaving.set(false);
    }
  }
}
