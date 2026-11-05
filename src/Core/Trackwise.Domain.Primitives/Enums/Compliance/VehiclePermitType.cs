using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>A paper a vehicle must keep current to run legally or be accepted by a client.</summary>
public enum VehiclePermitType
{
    /// <summary>The LTO Certificate of Registration and its Official Receipt (OR/CR), renewed yearly.</summary>
    [Description("LTO Registration")]
    LtoRegistration,

    /// <summary>The LTFRB certificate of public convenience - the franchise to haul for hire.</summary>
    [Description("LTFRB Franchise")]
    LtfrbFranchise,

    Insurance,

    /// <summary>National Meat Inspection Service accreditation, required to carry meat in a reefer.</summary>
    [Description("NMIS Accreditation")]
    NmisAccreditation,

    Other
}
