import { Component, inject } from "@angular/core";
import { PageHeader } from "@/shared/components";
import { WorkfilesTable } from "../components/workfiles-table/workfiles-table";
import { WorkfilesListStore } from "../store/workfiles-list.store";

@Component({
  selector: "app-workfiles-list",
  templateUrl: "./workfiles-list.html",
  providers: [WorkfilesListStore],
  imports: [PageHeader, WorkfilesTable],
})
export class WorkfilesListComponent {
  protected readonly store = inject(WorkfilesListStore);
}
