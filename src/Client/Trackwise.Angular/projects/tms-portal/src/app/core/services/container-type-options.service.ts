import { computed, inject, Injectable, signal } from "@angular/core";
import type { SelectOption } from "@trackwise/shared";
import {
  Api,
  getContainerTypeById,
  getContainerTypes,
  type ContainerTypeDto,
} from "@trackwise/shared/api";

/**
 * The master container-type list, as select options for the pages that reference one - containers,
 * tariffs and the quotation-to-booking conversion.
 *
 * Fetched once per session and shared, the way the retired `containerIsoTypeOptions` constant was:
 * five forms each running their own request would be five copies of the same fetch, and the list
 * changes about as often as ISO 6346 does.
 */
@Injectable({ providedIn: "root" })
export class ContainerTypeOptionsService {
  private readonly api = inject(Api);
  private readonly types = signal<ContainerTypeDto[]>([]);
  private fetchPromise: Promise<void> | null = null;

  public readonly options = computed<SelectOption<string>[]>(() =>
    this.types().map((type) => ({ label: this.label(type), value: type.id ?? "" })),
  );

  /**
   * Active types only: a retired box still resolves for the records naming it, but offering one
   * here would put new work through equipment nobody handles any more.
   */
  public async load(): Promise<void> {
    this.fetchPromise ??= this.fetch();
    return this.fetchPromise;
  }

  /**
   * Adds the type behind `id` when it is missing from the active list, so an edit form opened on a
   * record naming a retired type shows that type rather than a blank select the save would then
   * write back as a different box.
   */
  public async include(id: string | null | undefined): Promise<void> {
    await this.load();

    if (!id || this.types().some((type) => type.id === id)) {
      return;
    }

    try {
      const type = await this.api.invoke(getContainerTypeById, { id });
      if (type) {
        this.types.update((current) => [...current, type]);
      }
    } catch {
      // A type that cannot be read leaves the select empty, which is what the form already shows.
    }
  }

  private async fetch(): Promise<void> {
    try {
      const result = await this.api.invoke(getContainerTypes, {
        IsActive: true,
        OrderBy: "Code",
        PageSize: 200,
      });
      this.types.set(result.items ?? []);
    } catch (err) {
      console.error("Failed to load container types", err);
      this.fetchPromise = null;
    }
  }

  private label(type: ContainerTypeDto): string {
    return type.name ? `${type.name} (${type.code})` : (type.code ?? "");
  }
}
