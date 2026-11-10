import {
  CdkDrag,
  CdkDragHandle,
  CdkDropList,
  moveItemInArray,
  type CdkDragDrop,
} from "@angular/cdk/drag-drop";
import { DatePipe } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  model,
  signal,
  untracked,
} from "@angular/core";
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop";
import { FormsModule } from "@angular/forms";
import {
  ToastService,
  UiButton,
  UiDialog,
  UiTextField,
  type NoteSummaryDto,
} from "@trackwise/shared";
import { debounceTime, distinctUntilChanged, firstValueFrom } from "rxjs";
import { NotesApi } from "./notes-api";

const PAGE_SIZE = 20;

interface Draft {
  id: string | null;
  title: string;
  content: string;
}

@Component({
  selector: "app-sticky-notes",
  templateUrl: "./sticky-notes.html",
  imports: [
    UiDialog,
    UiButton,
    UiTextField,
    FormsModule,
    DatePipe,
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StickyNotes {
  private readonly api = inject(NotesApi);
  private readonly toastService = inject(ToastService);

  /** The list dialog. */
  public readonly open = model(false);

  protected readonly inset = { right: 16, bottom: 80 };
  protected readonly breakpoints = { "640px": "calc(100vw - 2rem)" };

  // list state
  protected readonly notes = signal<NoteSummaryDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly searchTerm = signal("");
  protected readonly pinnedOnly = signal(false);

  // editor state
  protected readonly editorOpen = signal(false);
  protected readonly draft = signal<Draft | null>(null);
  protected readonly saving = signal(false);
  protected readonly editorError = signal<string | null>(null);

  private readonly page = signal(1);
  private readonly totalPages = signal(1);

  protected readonly hasMore = computed(() => this.page() < this.totalPages());

  /** Reordering a filtered or partial view would write wrong display orders. */
  protected readonly canReorder = computed(() => !this.searchTerm() && !this.pinnedOnly());

  constructor() {
    // Load on every open so the list is never stale after being closed for a while.
    effect(() => {
      if (this.open()) untracked(() => void this.reload());
    });

    toObservable(this.searchTerm)
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        if (this.open()) void this.reload();
      });
  }

  // ---- queries ---------------------------------------------------------------------------------

  protected async reload(): Promise<void> {
    this.page.set(1);
    await this.fetchPage(1, false);
  }

  protected async loadMore(): Promise<void> {
    await this.fetchPage(this.page() + 1, true);
  }

  private async fetchPage(page: number, append: boolean): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const result = await firstValueFrom(
        this.api.list({
          SearchTerm: this.searchTerm().trim() || undefined,
          IsPinned: this.pinnedOnly() ? true : undefined,
          Page: page,
          PageSize: PAGE_SIZE,
        }),
      );
      const items = result.items ?? [];
      this.notes.update((current) => (append ? [...current, ...items] : items));
      this.page.set(result.pagination?.page ?? page);
      this.totalPages.set(result.pagination?.totalPages ?? 1);
    } catch {
      this.error.set("Couldn't load notes.");
    } finally {
      this.loading.set(false);
    }
  }

  protected togglePinnedFilter(): void {
    this.pinnedOnly.update((v) => !v);
    void this.reload();
  }

  // ---- editor dialog ---------------------------------------------------------------------------

  protected newNote(): void {
    this.editorError.set(null);
    this.draft.set({ id: null, title: "", content: "" });
    this.editorOpen.set(true);
  }

  /** Opens an existing note. The list only has a preview, so fetch the full content first. */
  protected async edit(note: NoteSummaryDto): Promise<void> {
    if (!note.id) return;
    this.error.set(null);
    this.editorError.set(null);
    try {
      const full = await firstValueFrom(this.api.get(note.id));
      this.draft.set({
        id: note.id,
        title: full.title ?? "",
        content: full.content ?? "",
      });
      this.editorOpen.set(true);
    } catch {
      this.error.set("Couldn't open that note.");
    }
  }

  protected patchDraft(patch: Partial<Draft>): void {
    this.draft.update((d) => (d ? { ...d, ...patch } : d));
  }

  protected onRowEnter(event: Event, note: NoteSummaryDto): void {
    if (event.target !== event.currentTarget) return;
    void this.edit(note);
  }
  /**
   * Fires for every close path (Cancel, X, Escape, after save). Clearing the draft here rather than
   * in `closeEditor` keeps the content on screen during the dialog's close animation.
   */
  protected onEditorClosed(): void {
    this.draft.set(null);
    this.editorError.set(null);
  }

  protected async save(): Promise<void> {
    const d = this.draft();
    if (!d) return;

    // Nothing written: treat as cancel rather than creating an empty sticky.
    if (!d.title.trim() && !d.content.trim()) {
      this.editorOpen.set(false);
      return;
    }

    this.saving.set(true);
    this.editorError.set(null);
    try {
      const body = { title: d.title.trim() || null, content: d.content };
      if (d.id) await firstValueFrom(this.api.update(d.id, body));
      else await firstValueFrom(this.api.create(body));

      this.editorOpen.set(false);
      await this.reload();
    } catch {
      // Stay open so the user's text isn't lost.
      this.editorError.set("Couldn't save the note.");
    } finally {
      this.saving.set(false);
    }
  }

  // ---- row actions -----------------------------------------------------------------------------

  protected async togglePin(note: NoteSummaryDto): Promise<void> {
    if (!note.id) return;
    try {
      await firstValueFrom(this.api.togglePin(note.id));
      // Pinning changes sort position server-side, so refetch rather than patching locally.
      await this.reload();
    } catch {
      this.error.set("Couldn't update the pin.");
    }
  }

  protected async confirmDelete(note: NoteSummaryDto): Promise<void> {
    this.toastService.confirmDelete(note.title || "Untitled", async () => {
      await this.remove(note);
    });
  }

  protected async remove(note: NoteSummaryDto): Promise<void> {
    if (!note.id) return;

    const previous = this.notes();
    this.notes.set(previous.filter((n) => n.id !== note.id)); // optimistic
    try {
      await firstValueFrom(this.api.delete(note.id));
    } catch {
      this.notes.set(previous);
      this.error.set("Couldn't delete the note.");
    }
  }

  protected async drop(event: CdkDragDrop<NoteSummaryDto[]>): Promise<void> {
    if (event.previousIndex === event.currentIndex) return;

    const reordered = [...this.notes()];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.notes.set(reordered);

    try {
      await firstValueFrom(
        this.api.reorder({
          items: reordered.map((n, displayOrder) => ({ id: n.id, displayOrder })),
        }),
      );
    } catch {
      this.error.set("Couldn't save the new order.");
      await this.reload();
    }
  }
}
