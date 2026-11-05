import { Component, computed, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import {
  Api,
  approvePartner,
  deletePartner,
  getPartnerById,
  rejectPartner,
  submitPartner,
  updatePartner,
  type BusinessPartnerDto,
  type UpdatePartnerCommand,
} from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import { Spinner, UiButton } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../components/master-data-history/master-data-history";
import { PartnerApproval } from "../components/partner-approval/partner-approval";
import { PartnerForm, type PartnerFormValue } from "../components/partner-form/partner-form";

/** The API's answer when the dependency probe stops a retirement. */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

/** The API's answer when an approver tries to decide their own submission. */
const SELF_APPROVAL = "MASTER_DATA_SELF_APPROVAL";

@Component({
  selector: "app-partner-edit",
  templateUrl: "./partner-edit.html",
  imports: [PageHeader, PartnerApproval, PartnerForm, MasterDataHistory, Spinner, UiButton],
})
export class PartnerEditComponent {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly permissions = inject(PermissionService);

  protected readonly id = this.route.snapshot.paramMap.get("id");

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);
  protected readonly canApprove = this.permissions.hasPermission(Permission.MasterData.Approve);

  protected readonly partner = signal<BusinessPartnerDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);

  /** Bumped after a save so the history panel re-reads instead of showing a stale trail. */
  protected readonly historyVersion = signal(0);

  /**
   * Deactivating is offered only on a partner that has been through approval. On a draft the flag
   * is already false and the way out of that is approval, not an edit - the server refuses it too.
   */
  protected readonly canDeactivate = computed(
    () => this.partner()?.approvalState === "approved" && this.partner()?.isActive === true,
  );

  protected readonly canReactivate = computed(
    () => this.partner()?.approvalState === "approved" && this.partner()?.isActive === false,
  );

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    if (!this.id) {
      return;
    }

    this.isLoading.set(true);
    try {
      this.partner.set(await this.api.invoke(getPartnerById, { id: this.id }));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected update(formValue: PartnerFormValue): Promise<void> {
    // Code is absent by design - it is fixed after creation. IsActive comes off the loaded record
    // rather than the form: publishing is approval's job, and the form must not offer a way round it.
    return this.send(
      () =>
        this.api.invoke(updatePartner, {
          id: this.id!,
          body: {
            name: formValue.name,
            roles: formValue.roles,
            address: formValue.address ?? undefined,
            contactName: formValue.contactName,
            email: formValue.email,
            phone: formValue.phone,
            taxId: formValue.taxId,
            branchId: formValue.branchId,
            notes: formValue.notes,
            isActive: this.partner()?.isActive ?? false,
          } satisfies UpdatePartnerCommand,
        }),
      "Partner updated",
      "Failed to update partner. Please check the information and try again.",
    );
  }

  protected submitForApproval(note: string | null): Promise<void> {
    return this.send(
      () => this.api.invoke(submitPartner, { id: this.id!, body: { note } }),
      "Sent for approval. Somebody other than you has to accept it before it goes live.",
      "Failed to submit the partner for approval.",
    );
  }

  protected approve(note: string | null): Promise<void> {
    return this.send(
      () => this.api.invoke(approvePartner, { id: this.id!, body: { note } }),
      "Approved. The partner is now live and selectable on workfiles.",
      "Failed to approve the partner.",
    );
  }

  protected reject(reason: string): Promise<void> {
    return this.send(
      () => this.api.invoke(rejectPartner, { id: this.id!, body: { reason } }),
      "Sent back to its steward.",
      "Failed to send the partner back.",
    );
  }

  protected setActive(isActive: boolean): Promise<void> {
    const partner = this.partner();

    if (!partner) {
      return Promise.resolve();
    }

    return this.send(
      () =>
        this.api.invoke(updatePartner, {
          id: this.id!,
          body: {
            name: partner.name,
            roles: partner.roles,
            address: partner.address ?? undefined,
            contactName: partner.contactName,
            email: partner.email,
            phone: partner.phone,
            taxId: partner.taxId,
            branchId: partner.branchId,
            notes: partner.notes,
            isActive,
          } satisfies UpdatePartnerCommand,
        }),
      isActive ? "Partner reactivated" : "Partner deactivated",
      "Failed to change the partner's status.",
    );
  }

  protected confirmDelete(): void {
    this.toastService.confirmDelete("business partner", () => void this.remove());
  }

  private async remove(): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await this.api.invoke(deletePartner, { id: this.id });

      this.toastService.showSuccess("Partner deleted. It can be restored from the partners list.");

      await this.router.navigateByUrl("/master-data/partners");
    } catch (error) {
      this.toastService.showError(this.apiMessage(error) ?? "Failed to delete partner.");
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * Every write on this page does the same three things around the call, and doing them by hand per
   * action is how one of them ends up missing the reload and showing a stale state badge.
   */
  private async send(
    call: () => Promise<unknown>,
    success: string,
    failure: string,
  ): Promise<void> {
    if (!this.id || this.isBusy()) {
      return;
    }

    this.isBusy.set(true);
    try {
      await call();
      this.toastService.showSuccess(success);

      await this.load();
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(this.apiMessage(error) ?? failure);
    } finally {
      this.isBusy.set(false);
    }
  }

  /**
   * The server's own sentence, for the failures where it says something the client cannot: what
   * still uses the record, or that the caller submitted it themselves. Both are decisions rather
   * than mistakes, and a generic message would hide which.
   */
  private apiMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE || body?.errorCode === SELF_APPROVAL
      ? (body.message ?? null)
      : null;
  }
}
