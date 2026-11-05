import {
  Component,
  effect,
  ElementRef,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from "@angular/core";
import type { FormValueControl } from "@angular/forms/signals";
import { focusFirstControl } from "@trackwise/shared";
import { Api, getLocationById, getLocations, type LocationDto } from "@trackwise/shared/api";
import { UiAutocompleteField } from "@trackwise/shared/ui";
import { formatTimeOfDay } from "@/shared/utils";

/**
 * Saved-site autocomplete: one customer's delivery locations plus the shared ones. Pass
 * `[locationId]` to seed it from a bare ID, as `app-search-truck` does with `truckId`.
 *
 * Implements `FormValueControl` only - see `text-field.ts` for the FormValueControl bridge contract.
 */
@Component({
  selector: "app-search-location",
  templateUrl: "./search-location.html",
  imports: [UiAutocompleteField],
})
export class SearchLocation implements FormValueControl<LocationDto | null> {
  private readonly api = inject(Api);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly suggestions = signal<LocationDto[]>([]);
  protected readonly lastQuery = signal("");
  protected readonly formatTimeOfDay = formatTimeOfDay;

  public readonly value = model<LocationDto | null>(null);
  public readonly disabled = input<boolean>(false);
  public readonly touch = output<void>();

  /** Whose sites to offer. Without one, only shared sites come back. */
  public readonly customerId = input<string | null>(null);

  public readonly locationId = input<string | null>(null);
  public readonly placeholder = input("Type an outlet name or code");

  public focus(options?: FocusOptions): void {
    focusFirstControl(this.host.nativeElement, options);
  }

  constructor() {
    effect(() => {
      const id = this.locationId();
      if (!id || untracked(this.value)?.id === id) {
        return;
      }
      void this.resolve(id);
    });
  }

  protected async search(event: { query: string }): Promise<void> {
    const query = event.query?.trim() ?? "";
    this.lastQuery.set(query);

    try {
      // Active only: a closed outlet keeps its past loads but is not offered for new ones.
      const result = await this.api.invoke(getLocations, {
        Search: query || undefined,
        CustomerId: this.customerId() ?? undefined,
        IncludeShared: true,
        IsActive: true,
        OrderBy: "Name",
        PageSize: 25,
      });

      // Zones group sites for pricing; nothing is delivered to one.
      this.suggestions.set((result.items ?? []).filter((l) => l.type !== "zone"));
    } catch {
      this.suggestions.set([]);
    }
  }

  private async resolve(id: string): Promise<void> {
    try {
      this.value.set(await this.api.invoke(getLocationById, { id }));
    } catch {
      // A site deleted since keeps the load's own address; the picker just starts empty.
    }
  }
}
