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
import {
  disabled,
  form,
  FormField,
  FormRoot,
  hidden,
  maxLength,
  minLength,
  pattern,
  required,
  validate,
} from "@angular/forms/signals";
import {
  Api,
  createDriverLicense,
  updateDriverLicense,
  type CreateDriverLicenseCommand,
  type DriverLicenseDto,
  type DriverLicenseStatus,
  type LicenseClass,
  type LicenseEndorsement,
  type PhilippineDlCode,
  type Region,
  type UpdateDriverLicenseCommand,
} from "@trackwise/shared/api";
import {
  driverLicenseStatusOptions,
  licenseClassesForCountry,
  licenseEndorsementOptions,
  philippineDlCodeOptions,
} from "@trackwise/shared/api/enums";
import { ToastService } from "@trackwise/shared/services";
import {
  Stack,
  UiButton,
  UiDateField,
  UiDialog,
  UiMultiSelectField,
  UiSelectField,
  UiTextField,
} from "@trackwise/shared/ui";
import { TenantService } from "@/core/services/tenant.service";
import { UiFormField, ValidatedForm } from "@/shared/components";

interface DriverLicenseModel {
  licenseNumber: string;
  licenseClass: LicenseClass;
  issuingCountry: string;
  issuingRegion: string;
  issuedDate: Date | null;
  expiresAt: Date | null;
  medicalCertExpiresAt: Date | null;
  status: DriverLicenseStatus;
  endorsements: LicenseEndorsement[];
  dlCodes: PhilippineDlCode[];
}

/** A new license starts on the tenant's own licensing system; EU tenants still pick the country. */
function emptyModel(region: Region | undefined): DriverLicenseModel {
  const [licenseClass, issuingCountry]: [LicenseClass, string] =
    region === "ph"
      ? ["ph_professional", "PH"]
      : region === "eu"
        ? ["euce", ""]
        : ["us_class_a", "US"];

  return {
    licenseNumber: "",
    licenseClass,
    issuingCountry,
    issuingRegion: "",
    issuedDate: null,
    expiresAt: null,
    medicalCertExpiresAt: null,
    status: "active",
    endorsements: [],
    dlCodes: [],
  };
}

const isPhilippine = (cls: LicenseClass) => cls.startsWith("ph_");
const isUs = (cls: LicenseClass) => cls.startsWith("us_");

@Component({
  selector: "app-driver-license-edit-dialog",
  templateUrl: "./driver-license-edit-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Stack,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiMultiSelectField,
    UiSelectField,
    UiTextField,
    ValidatedForm,
  ],
})
export class DriverLicenseEditDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly tenantService = inject(TenantService);

  readonly visible = model<boolean>(false);
  readonly employeeId = input.required<string>();
  readonly license = input<DriverLicenseDto | null>(null);
  readonly saved = output<void>();

  protected readonly statusOptions = driverLicenseStatusOptions;
  protected readonly endorsementOptions = licenseEndorsementOptions;
  protected readonly dlCodeOptions = philippineDlCodeOptions;

  protected readonly mode = computed<"create" | "update">(() =>
    this.license() ? "update" : "create",
  );
  protected readonly title = computed(() =>
    this.mode() === "create" ? "Add driver license" : "Edit driver license",
  );

  protected readonly model = signal<DriverLicenseModel>(emptyModel(undefined));

  protected readonly classOptions = computed(() =>
    licenseClassesForCountry(this.model().issuingCountry.toUpperCase()),
  );
  protected readonly isPhilippineClass = computed(() => isPhilippine(this.model().licenseClass));
  protected readonly isUsClass = computed(() => isUs(this.model().licenseClass));

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.licenseNumber, { message: "License number is required." });
      maxLength(p.licenseNumber, 64, {
        message: "License number cannot exceed 64 characters.",
      });
      // License number is immutable on update - disable the control declaratively.
      disabled(p.licenseNumber, { when: () => this.mode() === "update" });

      required(p.licenseClass, { message: "License class is required." });

      required(p.issuingCountry, { message: "Issuing country is required." });
      // Fixed after creation - the API ignores it on update and the class depends on it.
      disabled(p.issuingCountry, { when: () => this.mode() === "update" });
      minLength(p.issuingCountry, 2, {
        message: "Issuing country must be a 2-letter code.",
      });
      maxLength(p.issuingCountry, 2, {
        message: "Issuing country must be a 2-letter code.",
      });
      pattern(p.issuingCountry, /^[A-Za-z]{2}$/, {
        message: "Issuing country must be a 2-letter ISO code (e.g. PH, US, DE).",
      });

      required(p.issuedDate, { message: "Issued date is required." });
      required(p.expiresAt, { message: "Expiry date is required." });

      // Codes only exist on a Philippine license; hiding the field also skips this rule.
      hidden(p.dlCodes, { when: ({ valueOf }) => !isPhilippine(valueOf(p.licenseClass)) });
      validate(p.dlCodes, ({ value }) =>
        value().length > 0
          ? null
          : { kind: "dlCodes", message: "Pick the DL codes printed on the license (e.g. B2, C)." },
      );

      hidden(p.medicalCertExpiresAt, { when: ({ valueOf }) => !isUs(valueOf(p.licenseClass)) });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          // Hiding a control doesn't empty it, so drop values that don't apply to this class.
          const endorsements = isPhilippine(v.licenseClass) ? [] : (v.endorsements ?? []);
          const dlCodes = isPhilippine(v.licenseClass) ? v.dlCodes : [];
          const medicalCertExpiresAt = isUs(v.licenseClass)
            ? (v.medicalCertExpiresAt?.toISOString() ?? null)
            : null;

          try {
            const lic = this.license();
            if (lic?.id) {
              const command: UpdateDriverLicenseCommand = {
                licenseId: lic.id,
                licenseClass: v.licenseClass,
                endorsements,
                dlCodes,
                issuingRegion: v.issuingRegion || null,
                issuedDate: v.issuedDate?.toISOString(),
                expiresAt: v.expiresAt?.toISOString(),
                medicalCertExpiresAt,
                status: v.status,
              };
              await this.api.invoke(updateDriverLicense, {
                userId: this.employeeId(),
                licenseId: lic.id,
                body: command,
              });
              this.toast.showSuccess("Driver license updated");
            } else {
              const command: CreateDriverLicenseCommand = {
                employeeId: this.employeeId(),
                licenseNumber: v.licenseNumber,
                licenseClass: v.licenseClass,
                endorsements,
                dlCodes,
                issuingCountry: v.issuingCountry.toUpperCase(),
                issuingRegion: v.issuingRegion || null,
                issuedDate: v.issuedDate!.toISOString(),
                expiresAt: v.expiresAt!.toISOString(),
                medicalCertExpiresAt,
              };
              await this.api.invoke(createDriverLicense, {
                userId: this.employeeId(),
                body: command,
              });
              this.toast.showSuccess("Driver license added");
            }
            this.saved.emit();
            this.close();
          } catch {
            this.toast.showError("Failed to save driver license");
          }

          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const lic = this.license();
      if (this.visible()) {
        // Reset untracked: reading `this.form()` reactively here would re-run this effect on every
        // edit and clobber user input. The effect must depend only on `license()` / `visible()`.
        untracked(() =>
          this.form().reset(
            lic ? this.toModel(lic) : emptyModel(this.tenantService.tenantData()?.settings?.region),
          ),
        );
      }
    });

    // A class from another country's system would be rejected by the API, so switching the
    // country moves the class onto that country's first option.
    effect(() => {
      const options = this.classOptions();
      const current = untracked(() => this.model().licenseClass);
      if (options.length > 0 && !options.some((o) => o.value === current)) {
        untracked(() => this.model.update((m) => ({ ...m, licenseClass: options[0].value })));
      }
    });
  }

  close(): void {
    this.visible.set(false);
  }

  private toModel(lic: DriverLicenseDto): DriverLicenseModel {
    return {
      licenseNumber: lic.licenseNumber ?? "",
      licenseClass: lic.licenseClass ?? "us_class_a",
      issuingCountry: lic.issuingCountry ?? "US",
      issuingRegion: lic.issuingRegion ?? "",
      issuedDate: lic.issuedDate ? new Date(lic.issuedDate) : null,
      expiresAt: lic.expiresAt ? new Date(lic.expiresAt) : null,
      medicalCertExpiresAt: lic.medicalCertExpiresAt ? new Date(lic.medicalCertExpiresAt) : null,
      status: lic.status ?? "active",
      endorsements: lic.endorsements ?? [],
      dlCodes: lic.dlCodes ?? [],
    };
  }
}
