import { Component, computed, input } from "@angular/core";
import { type CustomerStatus } from "@trackwise/shared/api";
import { customerStatusOptions } from "@trackwise/shared/api/enums";
import { Badge, type IconName, type UiBadgeIntent } from "@trackwise/shared/ui";

@Component({
  selector: "app-customer-status-tag",
  templateUrl: "./customer-status-tag.html",
  imports: [Badge],
})
export class CustomerStatusTag {
  public readonly status = input.required<CustomerStatus>();

  protected readonly label = computed(() => {
    return customerStatusOptions.find((option) => option.value === this.status())?.label ?? "";
  });

  protected readonly severity = computed((): UiBadgeIntent => {
    switch (this.status()) {
      case "active":
        return "success";
      case "inactive":
        return "secondary";
      case "prospect":
        return "info";
      default:
        return "info";
    }
  });

  protected readonly icon = computed((): IconName => {
    switch (this.status()) {
      case "active":
        return "circle-check";
      case "inactive":
        return "circle-minus";
      case "prospect":
        return "star";
      default:
        return "circle";
    }
  });
}
