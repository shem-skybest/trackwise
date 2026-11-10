import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
  createNote,
  deleteNote,
  getNoteById,
  reorderNotes,
  toggleNotePin,
  updateNote,
  type CreateNote$Params,
  type CreateNoteRequest,
  type ReorderNotesRequest,
  type UpdateNoteRequest,
} from "@trackwise/shared";
import { ApiConfiguration } from "@trackwise/shared/api/generated/api-configuration";
import { getNotes, type GetNotes$Params } from "@trackwise/shared/api/generated/fn/note/get-notes";
import { map } from "rxjs";

@Injectable({ providedIn: "root" })
export class NotesApi {
  private readonly http = inject(HttpClient);
  private readonly rootUrl = inject(ApiConfiguration).rootUrl;

  list(params?: GetNotes$Params) {
    return getNotes(this.http, this.rootUrl, params).pipe(map((r) => r.body));
  }

  get(id: string) {
    return getNoteById(this.http, this.rootUrl, { id }).pipe(map((r) => r.body));
  }

  create(body: CreateNoteRequest) {
    return createNote(this.http, this.rootUrl, { body } satisfies CreateNote$Params).pipe(
      map((r) => r.body),
    );
  }

  update(id: string, body: UpdateNoteRequest) {
    return updateNote(this.http, this.rootUrl, { id, body }).pipe(map((r) => r.body));
  }

  delete(id: string) {
    return deleteNote(this.http, this.rootUrl, { id }).pipe(map(() => void 0));
  }

  togglePin(id: string) {
    return toggleNotePin(this.http, this.rootUrl, { id }).pipe(map((r) => r.body));
  }

  reorder(body: ReorderNotesRequest) {
    return reorderNotes(this.http, this.rootUrl, { body }).pipe(map(() => void 0));
  }
}
