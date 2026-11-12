import { Component, computed, inject, signal } from "@angular/core";
import { form, FormField, FormRoot, required } from "@angular/forms/signals";
import { Permission } from "@trackwise/shared";
import {
  Api,
  deleteXenditSettings,
  getXenditSettings,
  saveXenditSettings,
  type XenditSettingsDto,
} from "@trackwise/shared/api";
import {
  Alert,
  Badge,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiPasswordField,
  UiToggleField,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { TenantService } from "@/core/services/tenant.service";
import { environment } from "@/env";

/**
 * The company's own Xendit account. When it is on, invoice payment links open Xendit's hosted
 * page (cards, GCash, Maya, bank transfer, over-the-counter) and the money lands in the company's
 * Xendit balance. Xendit tells us a payment went through by calling the callback URL shown here.
 */
@Component({
  selector: "app-xendit-card",
  templateUrl: "./xendit-card.html",
  imports: [
    Alert,
    Badge,
    FormField,
    FormRoot,
    Icon,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiFormField,
    UiPasswordField,
    UiToggleField,
  ],
})
export class XenditCard {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly tenantService = inject(TenantService);

  protected readonly canManage = inject(PermissionService).hasPermission(Permission.Payment.Manage);
  protected readonly settings = signal<XenditSettingsDto | null>(null);
  protected readonly editing = signal(false);
  protected readonly savingActive = signal(false);
  protected readonly active = signal(true);

  /** Pasted into Xendit > Settings > Webhooks > Invoices paid. */
  protected readonly callbackUrl = computed(() => {
    const tenantId = this.tenantService.getTenantData()?.id;
    return tenantId ? `${environment.apiUrl}/webhooks/xendit/${tenantId}` : "";
  });

  protected readonly model = signal({ secretKey: "", callbackToken: "" });
  protected readonly form = form(
    this.model,
    (p) => {
      required(p.secretKey, {
        message: "Paste the secret key.",
        when: () => !this.settings()?.isConfigured,
      });
      required(p.callbackToken, {
        message: "Paste the callback verification token.",
        when: () => !this.settings()?.isConfigured,
      });
    },
    {
      submission: {
        action: async () => {
          await this.save(this.model().secretKey, this.model().callbackToken, this.active());
          return undefined;
        },
      },
    },
  );

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      const settings = await this.api.invoke(getXenditSettings);
      this.apply(settings);
    } catch {
      // The API error interceptor has already shown why.
    }
  }

  private apply(settings: XenditSettingsDto): void {
    this.settings.set(settings);
    this.active.set(settings.isConfigured ? (settings.isActive ?? true) : true);
    this.editing.set(!settings.isConfigured);
    this.form().reset({ secretKey: "", callbackToken: "" });
  }

  private async save(
    secretKey: string,
    callbackToken: string,
    isActive: boolean,
  ): Promise<boolean> {
    try {
      const settings = await this.api.invoke(saveXenditSettings, {
        body: {
          secretKey: secretKey.trim() || null,
          callbackToken: callbackToken.trim() || null,
          isActive,
        },
      });
      this.apply(settings);
      this.toast.showSuccess(
        settings.isActive
          ? "Xendit is connected. Payment links now open Xendit."
          : "Xendit saved but switched off. Payment links won't use it.",
      );
      return true;
    } catch {
      // The API error interceptor has already shown why (bad key, missing permission...).
      return false;
    }
  }

  protected async onActiveChange(isActive: boolean): Promise<void> {
    if (!this.settings()?.isConfigured) {
      return;
    }
    this.savingActive.set(true);
    const ok = await this.save("", "", isActive);
    if (!ok) {
      this.active.set(!isActive);
    }
    this.savingActive.set(false);
  }

  protected copyCallbackUrl(): void {
    void navigator.clipboard.writeText(this.callbackUrl()).then(() => {
      this.toast.showSuccess("Callback URL copied.");
    });
  }

  protected disconnect(): void {
    this.toast.confirm({
      message:
        "Remove the saved Xendit keys? Payment links stop opening Xendit. Payments already recorded stay.",
      accept: async () => {
        try {
          await this.api.invoke(deleteXenditSettings);
          this.apply({ isConfigured: false });
          this.toast.showSuccess("Xendit disconnected.");
        } catch {
          // Shown by the interceptor.
        }
      },
    });
  }
}
