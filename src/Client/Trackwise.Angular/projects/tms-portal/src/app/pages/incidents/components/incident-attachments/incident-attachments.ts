import { DatePipe } from "@angular/common";
import {
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  type OnDestroy,
} from "@angular/core";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  addIncidentAttachments,
  Api,
  deleteIncidentAttachment,
  downloadDocument,
  type DocumentType,
  type IncidentAttachmentDto,
  type IncidentDto,
} from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import {
  Card,
  Icon,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiLightbox,
  type UiLightboxImage,
} from "@trackwise/shared/ui";

const IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp"];
const PDF = "application/pdf";

/**
 * An incident's photos and papers - the crew's photos from Messenger, the police report or barangay
 * blotter. Paste (Ctrl+V), drop or browse; a PDF is filed as a report, an image as a photo.
 */
@Component({
  selector: "app-incident-attachments",
  templateUrl: "./incident-attachments.html",
  imports: [Card, DatePipe, Icon, Spinner, Stack, Typography, UiButton, UiLightbox],
})
export class IncidentAttachments implements OnDestroy {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly incident = input.required<IncidentDto>();
  public readonly canEdit = input(false);
  public readonly changed = output<IncidentDto>();

  protected readonly uploading = signal(false);
  protected readonly dragging = signal(false);
  protected readonly urls = signal<Record<string, string>>({});
  protected readonly lightboxOpen = signal(false);
  protected readonly lightboxIndex = signal(0);
  protected readonly accept = [...IMAGE_TYPES, PDF].join(",");

  private readonly attachments = computed(() => this.incident().attachments ?? []);
  protected readonly photos = computed(() =>
    this.attachments().filter((a) => IMAGE_TYPES.includes(a.contentType ?? "")),
  );
  protected readonly papers = computed(() =>
    this.attachments().filter((a) => !IMAGE_TYPES.includes(a.contentType ?? "")),
  );
  protected readonly images = computed<UiLightboxImage[]>(() =>
    this.photos().map((p) => ({ src: this.urls()[p.id!] ?? "", alt: p.fileName ?? "Photo" })),
  );

  constructor() {
    effect(() => void this.fetchPhotos(this.photos()));
  }

  ngOnDestroy(): void {
    Object.values(this.urls()).forEach((url) => URL.revokeObjectURL(url));
  }

  protected typeLabel(attachment: IncidentAttachmentDto): string {
    return attachment.type === "police_report" ? "Police report / blotter" : "Paper";
  }

  protected openPhoto(index: number): void {
    this.lightboxIndex.set(index);
    this.lightboxOpen.set(true);
  }

  protected async openPaper(attachment: IncidentAttachmentDto): Promise<void> {
    try {
      const blob = await this.api.invoke(downloadDocument, { documentId: attachment.id! });
      const url = URL.createObjectURL(blob);
      window.open(url, "_blank", "noopener");
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't open the file."));
    }
  }

  protected onPaste(event: ClipboardEvent): void {
    const files = Array.from(event.clipboardData?.files ?? []);
    if (files.length === 0) return;
    event.preventDefault();
    void this.upload(files);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    void this.upload(Array.from(event.dataTransfer?.files ?? []));
  }

  protected onBrowse(event: Event): void {
    const input = event.target as HTMLInputElement;
    void this.upload(Array.from(input.files ?? []));
    input.value = "";
  }

  protected remove(attachment: IncidentAttachmentDto): void {
    this.toast.confirm({
      header: "Remove file",
      message: `Remove ${attachment.fileName ?? "this file"} from the incident?`,
      icon: "warning",
      severity: "danger",
      acceptLabel: "Remove",
      accept: () => void this.delete(attachment),
    });
  }

  private async upload(files: File[]): Promise<void> {
    const images = files.filter((f) => IMAGE_TYPES.includes(f.type));
    const pdfs = files.filter((f) => f.type === PDF);
    if (images.length + pdfs.length < files.length) {
      this.toast.showInfo("Only photos (JPEG, PNG, WebP) and PDFs can be attached.");
    }

    this.uploading.set(true);
    try {
      let incident: IncidentDto | null = null;
      for (const [type, batch] of [
        ["photo", images],
        ["police_report", pdfs],
      ] as [DocumentType, File[]][]) {
        if (batch.length === 0) continue;
        incident = await this.api.invoke(addIncidentAttachments, {
          id: this.incident().id!,
          body: { Type: type, Files: batch },
        });
      }
      if (incident) {
        this.changed.emit(incident);
        this.toast.showSuccess("Attached");
      }
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't attach the files."));
    } finally {
      this.uploading.set(false);
    }
  }

  private async delete(attachment: IncidentAttachmentDto): Promise<void> {
    try {
      const incident = await this.api.invoke(deleteIncidentAttachment, {
        id: this.incident().id!,
        attachmentId: attachment.id!,
      });
      const url = this.urls()[attachment.id!];
      if (url) URL.revokeObjectURL(url);
      this.changed.emit(incident);
      this.toast.showSuccess("File removed");
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't remove the file."));
    }
  }

  /** Photos need the auth header, so they are downloaded and shown from object URLs. */
  private async fetchPhotos(photos: IncidentAttachmentDto[]): Promise<void> {
    const missing = photos.filter((p) => !this.urls()[p.id!]);
    const loaded = await Promise.all(
      missing.map(async (p) => {
        try {
          const blob = await this.api.invoke(downloadDocument, { documentId: p.id! });
          return [p.id!, URL.createObjectURL(blob)] as const;
        } catch {
          return null;
        }
      }),
    );
    const urls = Object.fromEntries(loaded.filter((e) => e !== null));
    if (Object.keys(urls).length) this.urls.update((u) => ({ ...u, ...urls }));
  }
}
