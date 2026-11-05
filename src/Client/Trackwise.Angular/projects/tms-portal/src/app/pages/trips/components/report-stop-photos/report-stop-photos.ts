import { DatePipe } from "@angular/common";
import { Component, computed, inject, input, signal } from "@angular/core";
import type { TripStopPhotoDto } from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import {
  Icon,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiLightbox,
  type UiLightboxImage,
} from "@trackwise/shared/ui";
import { TripPhotosStore } from "../../store/trip-photos.store";

const ACCEPTED_TYPES = ["image/jpeg", "image/png", "image/webp"];

/**
 * The photos on one block of the crew report: the loaded van at the warehouse, or proof at a drop.
 * The office pastes them straight from Messenger (Ctrl+V on the strip), drops them on it, or browses.
 */
@Component({
  selector: "app-report-stop-photos",
  templateUrl: "./report-stop-photos.html",
  imports: [DatePipe, Icon, Spinner, Stack, Typography, UiButton, UiLightbox],
})
export class ReportStopPhotos {
  private readonly toast = inject(ToastService);
  protected readonly store = inject(TripPhotosStore);

  public readonly blockKey = input.required<string>();
  /** Every stop the block covers - a warehouse visit's photo belongs to each load picked up. */
  public readonly stopIds = input.required<readonly string[]>();
  public readonly kind = input.required<"pickup" | "drop">();

  protected readonly accept = ACCEPTED_TYPES.join(",");
  protected readonly lightboxOpen = signal(false);
  protected readonly lightboxIndex = signal(0);
  protected readonly dragging = signal(false);

  protected readonly label = computed(() =>
    this.kind() === "pickup" ? "Loading photos" : "Delivery photos",
  );

  protected readonly photos = computed(() => {
    const ids = new Set(this.stopIds());
    return this.store.photos().filter((p) => p.stopIds?.some((id) => ids.has(id)));
  });

  protected readonly uploading = computed(() => this.store.uploading().includes(this.blockKey()));

  protected readonly images = computed<UiLightboxImage[]>(() =>
    this.photos().map((p) => ({ src: this.store.urls()[p.id!] ?? "", alt: p.fileName ?? "Photo" })),
  );

  protected open(index: number): void {
    this.lightboxIndex.set(index);
    this.lightboxOpen.set(true);
  }

  protected onPaste(event: ClipboardEvent): void {
    const files = Array.from(event.clipboardData?.files ?? []);
    if (files.length === 0) return;
    event.preventDefault();
    this.upload(files);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    this.upload(Array.from(event.dataTransfer?.files ?? []));
  }

  protected onBrowse(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.upload(Array.from(input.files ?? []));
    input.value = "";
  }

  protected remove(photo: TripStopPhotoDto): void {
    this.toast.confirm({
      header: "Remove photo",
      message: "Remove this photo from the report?",
      icon: "warning",
      severity: "danger",
      acceptLabel: "Remove",
      accept: () => void this.store.remove(photo),
    });
  }

  private upload(files: File[]): void {
    const photos = files.filter((f) => ACCEPTED_TYPES.includes(f.type));
    if (photos.length < files.length) {
      this.toast.showInfo("Only JPEG, PNG and WebP photos can be added.");
    }
    void this.store.add(this.blockKey(), [...this.stopIds()], photos);
  }
}
