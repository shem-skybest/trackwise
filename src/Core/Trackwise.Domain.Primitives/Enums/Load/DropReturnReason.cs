using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Why part of a drop came back on the truck. Drawn from the reasons Cold Chain's delivery remarks
/// actually give.
/// </summary>
public enum DropReturnReason
{
    Expired,

    [Description("Wrong PO / Barcode")]
    WrongPoOrBarcode,

    OverStock,
    FullStorage,
    Damaged,
    WrongAddress,
    Other
}
