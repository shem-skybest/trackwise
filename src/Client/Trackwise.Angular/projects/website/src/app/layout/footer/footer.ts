import { Component } from "@angular/core";
import { RouterLink } from "@angular/router";
import { Icon } from "@trackwise/shared/ui";

@Component({
  selector: "web-footer",
  templateUrl: "./footer.html",
  imports: [Icon, RouterLink],
})
export class Footer {
  protected readonly currentYear = new Date().getFullYear();
}
