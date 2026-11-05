import { Component, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, required } from "@angular/forms/signals";
import {
  UiButton,
  UiDialog,
  UiFormField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";

/**
 * Asks for the text a status step takes - review notes, the corrective action, why it is
 * reopened - and hands it back.
 */
@Component({
  selector: "app-incident-note-dialog",
  templateUrl: "./incident-note-dialog.html",
  imports: [FormField, FormRoot, UiButton, UiDialog, UiFormField, UiTextareaField, ValidatedForm],
})
export class IncidentNoteDialog {
  public readonly header = input.required<string>();
  public readonly label = input.required<string>();
  public readonly hint = input<string>("");
  public readonly confirmLabel = input("Save");
  public readonly isRequired = input(true);
  public readonly open = model(false);
  public readonly confirmed = output<string>();

  protected readonly model = signal({ text: "" });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.text, { message: "This is required.", when: () => this.isRequired() });
      maxLength(p.text, 4000, { message: "Keep it under 4,000 characters." });
    },
    {
      submission: {
        action: async () => {
          this.confirmed.emit(this.model().text.trim());
          return undefined;
        },
      },
    },
  );

  protected onOpened(): void {
    this.form().reset({ text: "" });
  }
}
