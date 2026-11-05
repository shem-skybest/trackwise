using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// An in-house system a master record can originate from. Stored on every cross-reference row so a
/// canonical record is always traceable back to the legacy row it was built from, and so a legacy
/// key keeps resolving after stewards merge duplicates.
/// </summary>
public enum MasterDataSourceSystem
{
    /// <summary>Created in the master-data module itself - no legacy ancestor.</summary>
    [Description("MDM")]
    Mdm = 0,

    /// <summary>The incumbent VB.NET freight system; the first and largest migration source.</summary>
    [Description("nSFS")]
    Nsfs = 1,

    /// <summary>Warehouse management.</summary>
    [Description("iWMS")]
    Iwms = 2,

    /// <summary>E-Request System.</summary>
    [Description("ERS")]
    Ers = 3,

    /// <summary>Skybest Accounting System.</summary>
    [Description("SAS")]
    Sas = 4,

    Kyowa = 5,

    [Description("iAMS")]
    Iams = 6,

    [Description("MCY")]
    Mcy = 7
}
