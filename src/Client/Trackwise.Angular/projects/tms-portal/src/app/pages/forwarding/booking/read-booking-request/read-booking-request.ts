import { Component, DestroyRef, inject, signal } from "@angular/core";
import { form, FormField, FormRoot, required } from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  readBookingRequest,
  type BookingRequestReadingDto,
  type CustomerDto,
  type ProposedColdChainTripDto,
} from "@trackwise/shared/api";
import {
  Alert,
  Card,
  Icon,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiFileUpload,
  UiFormField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { PageHeader, SearchCustomer } from "@/shared/components";
import {
  ProposedTripCard,
  type DropSitePick,
} from "../components/proposed-trip-card/proposed-trip-card";

const MAX_FILE_BYTES = 10 * 1024 * 1024;
const ACCEPTED_TYPES = ["image/png", "image/jpeg", "image/webp", "application/pdf"];

interface RequestModel {
  customer: CustomerDto | null;
  receivedOn: Date | null;
  messageText: string;
}

/**
 * Customer Service drops in a client's booking request - the Viber screenshot, an Excel table
 * pasted as an image, or the copied text - and gets back the trucks it asks for, each drop matched
 * to the client's saved locations. Saves nothing: booking from it waits on Cold Chain booking
 * creation coming back after the Booking refactor.
 */
@Component({
  selector: "app-read-booking-request",
  templateUrl: "./read-booking-request.html",
  host: { "(document:paste)": "onPaste($event)" },
  imports: [
    Alert,
    Card,
    FormField,
    FormRoot,
    Icon,
    PageHeader,
    ProposedTripCard,
    SearchCustomer,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiDateField,
    UiFileUpload,
    UiFormField,
    UiTextareaField,
    ValidatedForm,
  ],
})
export class ReadBookingRequest {
  private readonly api = inject(Api);

  protected readonly model = signal<RequestModel>({
    customer: null,
    receivedOn: new Date(),
    messageText: "",
  });
  protected readonly file = signal<File | null>(null);
  protected readonly previewUrl = signal<string | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly trips = signal<ProposedColdChainTripDto[]>([]);
  protected readonly warnings = signal<string[]>([]);
  protected readonly hasReading = signal(false);

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.customer, { message: "Pick the client who sent the request." });
      required(p.receivedOn, { message: "When did the message come in?" });
    },
    { submission: { action: () => this.read() } },
  );

  constructor() {
    inject(DestroyRef).onDestroy(() => this.revokePreview());
  }

  protected onFilesSelect(files: File[]): void {
    this.useFile(files[0] ?? null);
  }

  /** A screenshot copied with Win+Shift+S or from Viber can be pasted straight onto the page. */
  protected onPaste(event: ClipboardEvent): void {
    const image = Array.from(event.clipboardData?.items ?? [])
      .find((item) => item.kind === "file" && item.type.startsWith("image/"))
      ?.getAsFile();
    if (!image) return;

    event.preventDefault();
    const extension = image.type === "image/jpeg" ? "jpg" : (image.type.split("/")[1] ?? "png");
    this.useFile(new File([image], `pasted-screenshot.${extension}`, { type: image.type }));
  }

  protected clearFile(): void {
    this.useFile(null);
  }

  protected pickSite(tripIndex: number, pick: DropSitePick): void {
    this.trips.update((trips) =>
      trips.map((trip, t) =>
        t !== tripIndex
          ? trip
          : {
              ...trip,
              drops: (trip.drops ?? []).map((drop, d) =>
                d === pick.dropIndex ? { ...drop, site: pick.site ?? undefined } : drop,
              ),
            },
      ),
    );
  }

  private useFile(file: File | null): void {
    this.fileError.set(null);
    if (file && !ACCEPTED_TYPES.includes(file.type)) {
      this.fileError.set("Use a PNG, JPEG or WebP screenshot, or a PDF.");
      return;
    }
    if (file && file.size > MAX_FILE_BYTES) {
      this.fileError.set("The screenshot is over 10 MB.");
      return;
    }

    this.revokePreview();
    this.file.set(file);
    this.previewUrl.set(file?.type.startsWith("image/") ? URL.createObjectURL(file) : null);
  }

  private revokePreview(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
  }

  private async read(): Promise<undefined> {
    const { customer, receivedOn, messageText } = this.model();
    const file = this.file();
    this.error.set(null);

    if (!file && !messageText.trim()) {
      this.error.set("Add a screenshot or paste the client's message.");
      return undefined;
    }

    try {
      const reading: BookingRequestReadingDto = await this.api.invoke(readBookingRequest, {
        body: {
          CustomerId: customer!.id,
          ReceivedOn: toIsoDay(receivedOn!),
          File: file ?? undefined,
          MessageText: messageText.trim() || undefined,
        },
      });
      this.trips.set(reading.trips ?? []);
      this.warnings.set(reading.warnings ?? []);
      this.hasReading.set(true);
    } catch (err) {
      this.error.set(getApiErrorMessage(err, "Could not read the request."));
    }
    return undefined;
  }
}

/** The picked calendar day as YYYY-MM-DD, without a UTC shift moving it to the day before. */
function toIsoDay(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
