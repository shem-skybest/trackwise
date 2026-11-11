import { Component, viewChild } from "@angular/core";
import { Icon } from "@trackwise/shared/ui";
import { TrucksMap } from "@/shared/components";

// Shows the whole fleet, live, not the table's current page - so search and filters don't narrow it.
@Component({
  selector: "app-trucks-map-view",
  templateUrl: "./trucks-map-view.html",
  imports: [TrucksMap, Icon],
})
export class TrucksMapView {
  protected readonly map = viewChild(TrucksMap);
}
