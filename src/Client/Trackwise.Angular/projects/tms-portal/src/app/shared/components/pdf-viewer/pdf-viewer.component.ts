import { CommonModule } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  signal,
  untracked,
} from "@angular/core";
import { ToastService } from "@trackwise/shared";
import { NgxExtendedPdfViewerModule } from "ngx-extended-pdf-viewer";

/**
 * Fetches one document's PDF. The caller owns the knowledge of WHICH endpoint to call, so this
 * component never needs to change when a new document type (invoice, trip sheet...) is added:
 *
 *   protected readonly loadBookingPdf: PdfLoader = (id) => this.api.invoke(generateBookingPdf, { id });
 */
export type PdfLoader = (id: string) => Promise<Blob>;

@Component({
  selector: "app-pdf-viewer",
  standalone: true,
  imports: [CommonModule, NgxExtendedPdfViewerModule],
  templateUrl: "./pdf-viewer.component.html",
  styleUrl: "./pdf-viewer.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PdfViewerComponent {
  /** Id of the document to render (quotation id, booking id, ...). A change refetches. */
  readonly documentId = input.required<string>();

  /** Fetches the PDF for an id. Pass a stable reference (a class field), not a new function per render. */
  readonly loader = input.required<PdfLoader>();

  /** Suggested name when the user downloads from the viewer toolbar, e.g. "BK-2026-00042.pdf". */
  readonly fileName = input<string | null>(null);

  readonly loadingMessage = input("Generating document preview...");
  readonly errorFallback = input("Failed to generate the document preview.");
  readonly height = input("calc(100vh - 120px)");
  readonly showDownload = input(true);
  readonly showPrint = input(true);

  /** The inline error is always shown; set this when a toast on top of it is also wanted. */
  readonly toastOnError = input(false);

  private readonly toastService = inject(ToastService);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly pdfBlob = signal<Blob | null>(null);

  /** Guards against a slow earlier response overwriting a newer one when the id changes quickly. */
  private requestSeq = 0;

  constructor() {
    effect(() => {
      const id = this.documentId();
      // Only the id triggers a refetch; reading the loader or inputs here must not.
      untracked(() => {
        if (id) {
          void this.load(id);
        }
      });
    });
  }

  retry(): void {
    const id = this.documentId();
    if (id) {
      void this.load(id);
    }
  }

  private async load(id: string): Promise<void> {
    const seq = ++this.requestSeq;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const blob = await this.loader()(id);
      if (seq !== this.requestSeq) {
        return; // superseded by a newer request
      }

      // The viewer needs the PDF mime type; a generic blob from the client would not render.
      this.pdfBlob.set(
        blob.type === "application/pdf" ? blob : new Blob([blob], { type: "application/pdf" }),
      );
    } catch (error: unknown) {
      if (seq !== this.requestSeq) {
        return;
      }

      const message = await this.messageFrom(error);
      this.pdfBlob.set(null);
      this.errorMessage.set(message);

      if (this.toastOnError()) {
        this.toastService.showError(message);
      }
    } finally {
      if (seq === this.requestSeq) {
        this.isLoading.set(false);
      }
    }
  }

  private async messageFrom(error: unknown): Promise<string> {
    const fallback = this.errorFallback();

    if (error instanceof HttpErrorResponse) {
      const body: unknown = error.error;

      // A request made with responseType 'blob' also receives its error body as a Blob.
      if (body instanceof Blob) {
        try {
          const parsed = JSON.parse(await body.text()) as { message?: string; detail?: string };
          return parsed.message ?? parsed.detail ?? fallback;
        } catch {
          return fallback;
        }
      }

      if (error.status === 403) {
        return "You don't have permission to view this document.";
      }

      const message = (body as { message?: string } | null)?.message;
      return message ?? fallback;
    }

    return error instanceof Error ? error.message : fallback;
  }
}
