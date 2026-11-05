import { Component, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, required } from "@angular/forms/signals";
import {
  UiButton,
  UiDialog,
  UiFormField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";

/**
 * Asks for the one piece of text a batch step needs - the reason to unlock or cancel, or the
 * invoice number Accounting billed it under - and hands it back.
 */
@Component({
  selector: "app-batch-note-dialog",
  templateUrl: "./batch-note-dialog.html",
  imports: [
    FormField,
    FormRoot,
    UiButton,
    UiDialog,
    UiFormField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class BatchNoteDialog {
  public readonly header = input.required<string>();
  public readonly label = input.required<string>();
  public readonly hint = input<string>("");
  public readonly confirmLabel = input("Save");
  public readonly multiline = input(true);
  public readonly danger = input(false);
  public readonly open = model(false);
  public readonly confirmed = output<string>();

  protected readonly model = signal({ text: "" });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.text, { message: "This is required." });
      maxLength(p.text, 500, { message: "Keep it under 500 characters." });
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
    this.model.set({ text: "" });
  }
}
