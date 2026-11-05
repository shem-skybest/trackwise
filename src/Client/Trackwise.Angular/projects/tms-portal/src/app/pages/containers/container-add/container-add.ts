import { Component, inject, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  maxLength,
  minLength,
  pattern,
  required,
} from "@angular/forms/signals";
import { Router, RouterLink } from "@angular/router";
import {
  Api,
  createContainer,
  type CreateContainerCommand,
  type TerminalDto,
} from "@trackwise/shared/api";
import {
  Card,
  Container,
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiCheckboxField,
  UiFormField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ContainerTypeOptionsService, ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { SearchTerminal } from "@/shared/components/search";

interface ContainerFormModel {
  number: string;
  containerTypeId: string | null;
  isLaden: boolean;
  currentTerminal: TerminalDto | null;
  notes: string;
}

@Component({
  selector: "app-container-add",
  templateUrl: "./container-add.html",
  imports: [
    Card,
    Container,
    FormField,
    FormRoot,
    Grid,
    Icon,
    PageHeader,
    RouterLink,
    SearchTerminal,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiCheckboxField,
    UiFormField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class ContainerAdd {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly containerTypes = inject(ContainerTypeOptionsService);

  protected readonly containerTypeOptions = this.containerTypes.options;

  protected readonly model = signal<ContainerFormModel>({
    number: "",
    containerTypeId: null,
    isLaden: false,
    currentTerminal: null,
    notes: "",
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.number, { message: "Container number is required." });
      minLength(p.number, 11, {
        message: "Container number must be exactly 11 characters.",
      });
      maxLength(p.number, 11, {
        message: "Container number must be exactly 11 characters.",
      });
      pattern(p.number, /^[A-Z]{4}\d{7}$/, {
        message:
          "Container number must be 4 uppercase letters followed by 7 digits (e.g., MSCU1234567).",
      });
      required(p.containerTypeId, { message: "Container type is required." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const command: CreateContainerCommand = {
            number: v.number.toUpperCase(),
            containerTypeId: v.containerTypeId ?? undefined,
            isLaden: v.isLaden,
            currentTerminalId: v.currentTerminal?.id ?? null,
            notes: v.notes || null,
          };

          try {
            await this.api.invoke(createContainer, { body: command });
          } catch {
            this.toastService.showError("Failed to create container");
            return undefined;
          }
          this.toastService.showSuccess("A new container has been created successfully");
          this.router.navigateByUrl("/containers");
          return undefined;
        },
      },
    },
  );

  constructor() {
    void this.containerTypes.load();
  }
}
