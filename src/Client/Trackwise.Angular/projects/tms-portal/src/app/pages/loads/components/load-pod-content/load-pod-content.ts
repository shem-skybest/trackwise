import { CommonModule, DatePipe } from "@angular/common";
import { Component, effect, inject, input, signal } from "@angular/core";
import { Api, downloadDocument, getDocuments, type DocumentDto } from "@trackwise/shared/api";
import {
  Badge,
  Card,
  Icon,
  Spinner,
  UiButton,
  UiDataTable,
  UiDialog,
  UiTooltip,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { downloadBlobFile, formatFileSize } from "@trackwise/shared/utils";
import { ToastService } from "@/core/services";

/** What this tab lists: the driver's POD/BOL plus the loading photos from the crew report. */
const TYPE_LABELS = {
  proof_of_delivery: "Proof of Delivery",
  bill_of_lading: "Bill of Lading",
  pickup_inspection: "Loading Photo",
} as const;
type PodType = keyof typeof TYPE_LABELS;

@Component({
  selector: "app-load-pod-content",
  templateUrl: "./load-pod-content.html",
  imports: [
    Badge,
    Card,
    CommonModule,
    DatePipe,
    Icon,
    Spinner,
    UiButton,
    UiDataTable,
    UiDialog,
    UiTooltip,
  ],
})
export class LoadPodContent {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  readonly loadId = input.required<string>();
  protected readonly isLoading = signal(false);
  protected readonly documents = signal<DocumentDto[]>([]);
  protected readonly selectedDocument = signal<DocumentDto | null>(null);
  protected readonly showDetailDialog = signal(false);

  constructor() {
    effect(() => {
      const id = this.loadId();
      if (id) {
        this.loadDocuments(id);
      }
    });
  }

  protected refresh(): void {
    const id = this.loadId();
    if (id) {
      this.loadDocuments(id);
    }
  }

  private async loadDocuments(loadId: string): Promise<void> {
    this.isLoading.set(true);

    const result = await this.api.invoke(getDocuments, {
      OwnerType: "load",
      OwnerId: loadId,
    });

    if (result) {
      const filtered = result.filter((doc) => doc.type && doc.type in TYPE_LABELS);
      this.documents.set(filtered);
    }
    this.isLoading.set(false);
  }

  protected viewDetails(doc: DocumentDto): void {
    this.selectedDocument.set(doc);
    this.showDetailDialog.set(true);
  }

  protected async download(doc: DocumentDto): Promise<void> {
    try {
      const blob = await this.api.invoke(downloadDocument, { documentId: doc.id! });
      const fileName = doc.originalFileName || doc.fileName;
      downloadBlobFile(blob, fileName!);
    } catch {
      this.toast.showError("Failed to download file");
    }
  }

  protected readonly formatFileSize = formatFileSize;

  protected getTypeLabel(type?: string): string {
    return TYPE_LABELS[type as PodType] ?? "Document";
  }

  protected getTypeSeverity(type?: string): UiBadgeIntent {
    return type === "bill_of_lading" ? "info" : "success";
  }

  protected formatCoordinates(
    lat: number | null | undefined,
    lng: number | null | undefined,
  ): string {
    if (lat == null || lng == null) return "N/A";
    return `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
  }

  protected getGoogleMapsUrl(
    lat: number | null | undefined,
    lng: number | null | undefined,
  ): string {
    if (lat == null || lng == null) return "";
    return `https://www.google.com/maps?q=${lat},${lng}`;
  }

  protected isImage(doc: DocumentDto): boolean {
    return doc.contentType?.startsWith("image/") || false;
  }
}
