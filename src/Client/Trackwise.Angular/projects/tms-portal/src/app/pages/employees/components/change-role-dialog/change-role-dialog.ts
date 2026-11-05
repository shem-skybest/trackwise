import { Component, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, required } from "@angular/forms/signals";
import { UiSelectField } from "@trackwise/shared";
import { type RoleDto } from "@trackwise/shared/api";
import { Spinner, UiButton, UiDialog } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { UserService } from "../../services";

const EMPTY = { role: "" };

@Component({
  selector: "app-change-role-dialog",
  templateUrl: "./change-role-dialog.html",
  imports: [FormField, FormRoot, Spinner, UiButton, UiDialog, UiSelectField],
})
export class ChangeRoleDialog {
  private readonly userService = inject(UserService);
  private readonly toastService = inject(ToastService);

  public readonly userId = input.required<string>();
  public readonly currentRole = input<RoleDto | null>(null);
  public readonly visible = model<boolean>(false);
  public readonly roleChanged = output<void>();

  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly isLoading = signal(false);

  protected readonly model = signal({ ...EMPTY });

  /**
   * An empty `role` on submit runs `onInvalid`, which shows the "select a role" toast - this dialog
   * has no inline `ui-form-field` error slot to surface the error otherwise.
   */
  protected readonly form = form(
    this.model,
    (p) => {
      required(p.role, { message: "Select a role from the list" });
    },
    {
      submission: {
        // TODO: `PUT /employees/{userId}` (role assignment) was removed when EmployeeController
        // was trimmed to read-only for the Master Data promotion (2026-09-18) - there is currently
        // no backend endpoint for this. Stubbed rather than left calling a function that no longer
        // exists, so the rest of the app can build; role assignment needs a new home (back on
        // EmployeeController, or elsewhere) before this dialog can work again.
        action: async () => {
          this.toastService.showError(
            "Role changes are temporarily unavailable - the backend endpoint was removed.",
          );
          return undefined;
        },
        onInvalid: () => {
          this.toastService.showError("Select a role from the list");
        },
      },
    },
  );

  constructor() {
    this.fetchRoles();
  }

  close(): void {
    this.visible.set(false);
    this.clearSelectedRole();
  }

  clearSelectedRole(): void {
    this.model.set({ ...EMPTY });
  }

  async removeRole(): Promise<void> {
    // See the submit action above - same removed backend endpoint.
    this.toastService.showError(
      "Role changes are temporarily unavailable - the backend endpoint was removed.",
    );
  }

  private fetchRoles(): void {
    this.userService.fetchRoles().subscribe((roles) => {
      if (roles) {
        this.roles.set(roles);
      }
    });
  }
}
