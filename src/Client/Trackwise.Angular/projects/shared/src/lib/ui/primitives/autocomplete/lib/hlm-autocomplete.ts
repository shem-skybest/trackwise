import { Directive } from "@angular/core";
import { BrnAutocomplete } from "@spartan-ng/brain/autocomplete";
import {
  BrnPopover,
  provideBrnPopoverConfig,
  provideBrnPopoverDefaultOptions,
} from "@spartan-ng/brain/popover";
import { classes } from "../../utils";

@Directive({
  selector: "[hlmAutocomplete],hlm-autocomplete",
  providers: [
    provideBrnPopoverConfig({
      align: "start",
      sideOffset: 6,
    }),
    // A combobox never moves real DOM focus off the input - navigation is virtual via
    // aria-activedescendant. Without this, the overlay's default autoFocus steals focus onto the
    // first tabbable element inside the panel, e.g. an `#empty` template's "Add new X" button.
    provideBrnPopoverDefaultOptions({ role: null, autoFocus: false }),
  ],
  hostDirectives: [
    {
      directive: BrnAutocomplete,
      inputs: [
        "autoHighlight",
        "disabled",
        "value",
        "search",
        "itemToString",
        "isItemEqualToValue",
      ],
      outputs: ["valueChange", "searchChange"],
    },
    {
      directive: BrnPopover,
      inputs: ["align", "closeOnOutsidePointerEvents", "sideOffset", "state", "offsetX"],
      outputs: ["stateChanged", "closed"],
    },
  ],
  host: { "data-slot": "autocomplete" },
})
export class HlmAutocomplete {
  constructor() {
    classes(() => "block");
  }
}
