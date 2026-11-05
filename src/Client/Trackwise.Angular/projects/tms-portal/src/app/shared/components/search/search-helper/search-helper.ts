import { HttpContext } from "@angular/common/http";
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
import { focusFirstControl, isEmptyGuid } from "@trackwise/shared";
import { Api, getHelperById, getHelpers, SKIP_CACHE, type HelperDto } from "@trackwise/shared/api";
import { UiAutocompleteField } from "@trackwise/shared/ui";

/**
 * Component for searching and selecting a helper (the laborer who rides a trip alongside the
 * driver). Same shape as `SearchTruck` - an autocomplete over the master-data list, always
 * yielding a `HelperDto`; pass `[helperId]` to seed it from a bare ID.
 *
 * Implements `FormValueControl` only - see `text-field.ts` for the FormValueControl bridge contract.
 */
@Component({
  selector: "app-search-helper",
  templateUrl: "./search-helper.html",
  imports: [UiAutocompleteField],
})
export class SearchHelper implements FormValueControl<HelperDto | null> {
  private readonly api = inject(Api);

  protected readonly suggestedHelpers = signal<HelperDto[]>([]);

  /** The control's value. Required by `FormValueControl`. */
  public readonly value = model<HelperDto | null>(null);

  /** Seeds the control from a bare helper ID - see `SearchTruck.truckId` for why this is separate
   * from `value` rather than widening its type. */
  public readonly helperId = input<string | null>(null);

  /**
   * An ISO date: offer only helpers who can be put on a truck that day, leaving out anyone whose
   * crew status is absent, on leave, suspended, training or lent out.
   */
  public readonly availableOn = input<string | null>(null);

  /** Driven by the Reactive Forms bridge / consumers to disable the input. */
  public readonly disabled = input<boolean>(false);

  /** Raised on blur so the form can mark the field touched. */
  public readonly touch = output<void>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Signal Forms calls this via `FieldState.focusBoundControl()`. */
  public focus(options?: FocusOptions): void {
    focusFirstControl(this.host.nativeElement, options);
  }

  constructor() {
    effect(() => {
      const id = this.helperId();
      if (!id || isEmptyGuid(id)) {
        return;
      }
      if (untracked(this.value)?.id === id) {
        return;
      }
      this.resolveHelperById(id);
    });
  }

  protected async searchHelper(event: { query: string }): Promise<void> {
    const availableOn = this.availableOn();
    const result = await this.api.invoke(
      getHelpers,
      { Search: event.query, IsActive: true, ...(availableOn ? { AvailableOn: availableOn } : {}) },
      // Filtered by the day's crew status, which a /crew-assignments write changes.
      availableOn ? new HttpContext().set(SKIP_CACHE, true) : undefined,
    );
    this.suggestedHelpers.set(result.items ?? []);
  }

  /** Marks the control as touched so validation errors surface (on blur). */
  protected markTouched(): void {
    this.touch.emit();
  }

  private async resolveHelperById(id: string): Promise<void> {
    const result = await this.api.invoke(getHelperById, { id });
    if (result) {
      this.value.set(result);
    }
  }
}
