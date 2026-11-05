import { Component, effect, inject, input, model, signal, untracked } from "@angular/core";
import { form, FormField, FormRoot, minLength, required, validate } from "@angular/forms/signals";
import { Api, setEmployeePassword } from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import { Stack, Typography, UiButton, UiDialog, UiPasswordField } from "@trackwise/shared/ui";
import { UiFormField, ValidatedForm } from "@/shared/components";

const EMPTY = { password: "", confirm: "" };

/**
 * Gives an employee a starting password for staff with no reachable inbox - drivers sit on
 * placeholder emails, so the forgot-password link can never reach them.
 */
@Component({
  selector: "app-set-password-dialog",
  templateUrl: "./set-password-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Stack,
    Typography,
    UiButton,
    UiDialog,
    UiFormField,
    UiPasswordField,
    ValidatedForm,
  ],
})
export class SetPasswordDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  readonly visible = model<boolean>(false);
  readonly employeeId = input.required<string>();
  readonly employeeName = input<string>("");
  readonly loginEmail = input<string>("");

  protected readonly model = signal({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.password, { message: "Enter a password." });
      minLength(p.password, 8, { message: "Use at least 8 characters." });
      validate(p.confirm, ({ value, valueOf }) =>
        value() === valueOf(p.password)
          ? null
          : { kind: "mismatch", message: "The passwords don't match." },
      );
    },
    {
      submission: {
        action: async () => {
          try {
            await this.api.invoke(setEmployeePassword, {
              userId: this.employeeId(),
              body: { userId: this.employeeId(), password: this.model().password },
            });
            this.toast.showSuccess(`Password set. ${this.employeeName()} can sign in now.`);
            this.close();
          } catch {
            // The global error interceptor already shows the server's reason.
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      if (this.visible()) {
        untracked(() => this.form().reset({ ...EMPTY }));
      }
    });
  }

  close(): void {
    this.visible.set(false);
  }
}
