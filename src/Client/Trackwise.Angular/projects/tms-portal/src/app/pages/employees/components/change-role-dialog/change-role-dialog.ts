import {
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from "@angular/core";
import { form, FormField, FormRoot, required } from "@angular/forms/signals";
import { UiSelectField } from "@trackwise/shared";
import { Api, assignEmployeeRole, type RoleDto } from "@trackwise/shared/api";
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
  private readonly api = inject(Api);
  private readonly userService = inject(UserService);
  private readonly toastService = inject(ToastService);

  public readonly userId = input.required<string>();
  public readonly currentRole = input<RoleDto | null>(null);
  public readonly visible = model<boolean>(false);
  public readonly roleChanged = output<void>();

  private readonly assignableRoles = signal<RoleDto[]>([]);
  protected readonly isLoading = signal(false);

  protected readonly roles = computed(() =>
    this.assignableRoles().filter((role) => role.name !== this.currentRole()?.name),
  );

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
        action: async () => {
          try {
            await this.api.invoke(assignEmployeeRole, {
              userId: this.userId(),
              body: { employeeId: this.userId(), roleName: this.model().role },
            });
            const label = this.roles().find((r) => r.name === this.model().role)?.displayName;
            this.toastService.showSuccess(
              `Role changed to ${label}. It applies the next time they sign in.`,
            );
            this.roleChanged.emit();
            this.close();
          } catch {
            // The global error interceptor already shows the server's reason.
          }
          return undefined;
        },
        onInvalid: () => {
          this.toastService.showError("Select a role from the list");
        },
      },
    },
  );

  constructor() {
    effect(() => {
      if (this.visible()) {
        untracked(() => this.fetchRoles());
      }
    });
  }

  close(): void {
    this.visible.set(false);
    this.model.set({ ...EMPTY });
  }

  private fetchRoles(): void {
    this.isLoading.set(true);
    this.userService.fetchRoles().subscribe({
      next: (roles) => this.assignableRoles.set(roles),
      complete: () => this.isLoading.set(false),
      error: () => this.isLoading.set(false),
    });
  }
}
