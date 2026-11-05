import { Component, inject } from "@angular/core";
import { Router } from "@angular/router";
import { PageHeader } from "@/shared/components";
import { QuotationsTable } from "../components/quotations-table/quotations-table";
import { QuotationsListStore } from "../store/quotations-list.store";

@Component({
  selector: "app-quotations-list",
  templateUrl: "./quotations-list.html",
  providers: [QuotationsListStore],
  imports: [PageHeader, QuotationsTable],
})
export class QuotationsListComponent {
  private readonly router = inject(Router);

  protected readonly store = inject(QuotationsListStore);

  protected addQuotation(): void {
    this.router.navigate(["/quotations/add"]);
  }
}
