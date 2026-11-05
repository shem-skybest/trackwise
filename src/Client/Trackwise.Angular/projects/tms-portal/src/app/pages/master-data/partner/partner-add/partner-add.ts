import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, createPartner, type CreatePartnerCommand } from "@trackwise/shared/api";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { PartnerForm, type PartnerFormValue } from "../components/partner-form/partner-form";

@Component({
  selector: "app-partner-add",
  templateUrl: "./partner-add.html",
  imports: [PageHeader, PartnerForm],
})
export class PartnerAddComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(false);

  protected async create(formValue: PartnerFormValue): Promise<void> {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const command: CreatePartnerCommand = {
      code: formValue.code ?? undefined,
      name: formValue.name,
      roles: formValue.roles,
      address: formValue.address ?? undefined,
      contactName: formValue.contactName,
      email: formValue.email,
      phone: formValue.phone,
      taxId: formValue.taxId,
      branchId: formValue.branchId,
      notes: formValue.notes,
    };

    try {
      const partner = await this.api.invoke(createPartner, { body: command });

      // Said plainly, because the difference between this and every other master list is the whole
      // point: nothing has been published, and the steward has one more step to take.
      this.toastService.showSuccess(
        `Partner ${partner.code} saved as a draft. Submit it for approval to make it usable.`,
      );

      await this.router.navigateByUrl(`/master-data/partners/${partner.id}`);
    } catch (error) {
      // A 409 is the duplicate probe, not a malformed form: the steward is being shown an existing
      // partner rather than told they typed something wrong.
      this.toastService.showError(
        (error as { error?: { errorCode?: string; message?: string } })?.error?.message ??
          "Failed to create partner. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
