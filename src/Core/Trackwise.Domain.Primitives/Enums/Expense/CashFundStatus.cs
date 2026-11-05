namespace Trackwise.Domain.Primitives.Enums;

public enum CashFundStatus
{
    /// <summary>Being spent and liquidated: lines can still be added, forwarded and reviewed.</summary>
    Open = 0,

    /// <summary>Every receipt reviewed and the balance settled; locked until reopened.</summary>
    Liquidated = 1
}
