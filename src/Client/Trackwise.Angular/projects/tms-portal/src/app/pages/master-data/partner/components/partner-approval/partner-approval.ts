import { DatePipe } from "@angular/common";
import { Component, computed, input, output, signal } from "@angular/core";
import type { BusinessPartnerDto } from "@trackwise/shared/api";
import {
  Badge,
  Card,
  Icon,
  Stack,
  Typography,
  UiButton,
  UiFormField,
  UiTextareaField,
} from "@trackwise/shared/ui";

/**
 * The maker-checker controls for one partner: submit, approve, send back.
 * <p>
 * Its own component rather than three buttons in the page header, because what a steward needs here
 * is not a button - it is the answer to "where is this and who is it with", and the buttons are
 * what that answer permits. Rates are the next entity to need exactly this panel.
 */
@Component({
  selector: "app-partner-approval",
  templateUrl: "./partner-approval.html",
  imports: [Badge, Card, DatePipe, Icon, Stack, Typography, UiButton, UiFormField, UiTextareaField],
})
export class PartnerApproval {
  public readonly partner = input.required<BusinessPartnerDto | null>();
  public readonly canManage = input(false);
  public readonly canApprove = input(false);
  public readonly isBusy = input(false);

  public readonly submitForApproval = output<string | null>();
  public readonly approve = output<string | null>();
  public readonly reject = output<string>();

  protected readonly note = signal("");

  /** Revealed only when the approver clicks Send back - a rejection must carry a reason. */
  protected readonly rejecting = signal(false);

  protected readonly state = computed(() => this.partner()?.approvalState ?? "draft");

  protected readonly canReject = computed(() => this.note().trim().length > 0);

  protected onSubmit(): void {
    this.submitForApproval.emit(this.note().trim() || null);
    this.reset();
  }

  protected onApprove(): void {
    this.approve.emit(this.note().trim() || null);
    this.reset();
  }

  protected startRejecting(): void {
    this.rejecting.set(true);
  }

  protected onReject(): void {
    const reason = this.note().trim();

    if (reason.length === 0) {
      return;
    }

    this.reject.emit(reason);
    this.reset();
  }

  protected cancelRejecting(): void {
    this.reset();
  }

  private reset(): void {
    this.note.set("");
    this.rejecting.set(false);
  }
}
