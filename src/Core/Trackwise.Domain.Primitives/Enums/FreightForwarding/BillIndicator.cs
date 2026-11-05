using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Whether the file is worked against the master or the house bill. Required on every type except
/// Reefer, which carries no bill at all.
/// </summary>
public enum BillIndicator
{
    [Description("MBL")]
    Mbl = 0,

    [Description("HBL")]
    Hbl = 1
}
