namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// The four entity groups the master-data module is organised around. A record's group decides
/// which steward permission governs it, whether a change needs maker-checker approval before it
/// publishes, and where it appears in the module's navigation.
/// </summary>
public enum MasterDataGroup
{
    /// <summary>Ports, locations, vessels, commodities, currencies, branches and the generic lookup lists.</summary>
    ReferenceList = 0,

    /// <summary>Customers, shippers, consignees, vendors, brokers, garages - anything with a role model.</summary>
    BusinessPartner = 1,

    /// <summary>Trucks, trailers, drivers, helpers and the policies and registrations attached to them.</summary>
    FleetAndPersonnel = 2,

    /// <summary>Charge codes, tariffs, shipping and reefer rates, tolls and cost factors.</summary>
    RateAndCharge = 3
}
