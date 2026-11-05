using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

public enum BillingInterval
{
    [Description("Daily")] Day,
    [Description("Weekly")] Week,
    [Description("Monthly")] Month,
    [Description("Yearly")] Year
}
