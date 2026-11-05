import { Component, computed, inject } from "@angular/core";
import { RouterLink } from "@angular/router";
import { FeatureService } from "@trackwise/shared/services";
import { Card, Icon, PageHeader, type IconName } from "@trackwise/shared/ui";

interface ReportCard {
  title: string;
  description: string;
  icon: IconName;
  route: string;
}

interface ReportGroup {
  label: string;
  cards: ReportCard[];
}

@Component({
  selector: "app-reports-hub",
  templateUrl: "./reports-hub.html",
  imports: [RouterLink, Card, Icon, PageHeader],
})
export class ReportsHubComponent {
  private readonly featureService = inject(FeatureService);

  protected readonly groups = computed<ReportGroup[]>(() => {
    const financial: ReportCard[] = [
      {
        title: "Financial Report",
        description: "Profit, loss, and cash flow at a glance.",
        icon: "chart-line",
        route: "/reports/financials",
      },
      {
        title: "Revenue Overview",
        description: "Revenue trends across periods and lanes.",
        icon: "trending-up",
        route: "/reports/revenue",
      },
      {
        title: "Payroll Report",
        description: "Driver and staff payroll totals per period.",
        icon: "wallet",
        route: "/reports/payroll",
      },
    ];

    if (this.featureService.isEnabled("ifta")) {
      financial.push({
        title: "IFTA Report",
        description: "Quarterly fuel tax by jurisdiction.",
        icon: "receipt",
        route: "/reports/ifta",
      });
    }

    return [
      {
        label: "Operations",
        cards: [
          {
            title: "Loads",
            description: "Volume, status, and delivery performance.",
            icon: "package",
            route: "/reports/loads",
          },
          {
            title: "Drivers",
            description: "Driver activity and productivity summary.",
            icon: "users",
            route: "/reports/drivers",
          },
          {
            title: "Drivers Detailed",
            description: "Per-driver breakdown of miles and loads.",
            icon: "id-card",
            route: "/reports/drivers/detailed",
          },
          {
            title: "Team Overview",
            description: "How the whole team is performing together.",
            icon: "briefcase",
            route: "/reports/team",
          },
        ],
      },
      {
        label: "Financial",
        cards: financial,
      },
      {
        label: "Fleet",
        cards: [
          {
            title: "Safety Report",
            description: "DVIR, incidents, and compliance signals.",
            icon: "shield",
            route: "/reports/safety",
          },
          {
            title: "Maintenance Report",
            description: "Service history and upcoming maintenance.",
            icon: "wrench",
            route: "/reports/maintenance",
          },
        ],
      },
    ];
  });
}
