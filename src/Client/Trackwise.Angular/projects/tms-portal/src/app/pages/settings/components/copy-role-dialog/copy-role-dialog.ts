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
import { form, FormField, FormRoot, maxLength, required } from "@angular/forms/signals";
import { UiSelectField } from "@trackwise/shared";
import { Api, createTenantRole, type RoleDto } from "@trackwise/shared/api";
import { UiButton, UiDialog, UiTextField, ValidatedForm } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { UiFormField } from "@/shared/components";

const EMPTY = { displayName: "", copyFrom: "" };

/**
 * Makes a new role by copying an existing one (e.g. "CS + Dispatch Cebu" from "CS + Dispatch"),
 * which is then edited like any other. Starting from a copy rather than an empty role keeps a
 * new role from silently missing the basics everyone needs.
 */
@Component({
  selector: "app-copy-role-dialog",
  templateUrl: "./copy-role-dialog.html",
  imports: [
    FormField,
    FormRoot,
    UiButton,
    UiDialog,
    UiFormField,
    UiSelectField,
    UiTextField,
    ValidatedForm,
  ],
})
export class CopyRoleDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly visible = model(false);
  public readonly roles = input<RoleDto[]>([]);
  public readonly defaultSource = input<string | null>(null);
  public readonly copied = output<string>();

  /** Only roles the caller could hand out are copyable - the API refuses the rest anyway. */
  protected readonly sources = computed(() =>
    this.roles().filter((r) => r.canAssign && r.name !== "tenant.owner"),
  );

  protected readonly model = signal({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.displayName, { message: "Give the new role a name." });
      maxLength(p.displayName, 60, { message: "Keep the name under 60 characters." });
      required(p.copyFrom, { message: "Pick the role to copy." });
    },
    {
      submission: {
        action: async () => {
          try {
            const roleName = await this.api.invoke(createTenantRole, {
              body: {
                displayName: this.model().displayName,
                copyFromRoleName: this.model().copyFrom,
              },
            });
            this.toast.showSuccess(
              `${this.model().displayName} created. Adjust its permissions next.`,
            );
            this.copied.emit(roleName);
            this.visible.set(false);
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
        untracked(() => {
          const source = this.sources().some((r) => r.name === this.defaultSource())
            ? this.defaultSource()!
            : "";
          this.form().reset({ ...EMPTY, copyFrom: source });
        });
      }
    });
  }
}
