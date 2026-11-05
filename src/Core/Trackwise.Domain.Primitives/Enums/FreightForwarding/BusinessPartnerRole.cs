namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// A capacity a <c>BusinessPartner</c> can act in on a freight-forwarding file. One partner may
/// hold several - a trucker that also runs a depot is one record with two roles, not two records.
/// </summary>
public enum BusinessPartnerRole
{
    ShippingLine = 0,
    CoLoader = 1,
    Forwarder = 2,
    Trucker = 3,
    Broker = 4,
    Agent = 5,
    Shipper = 6,
    Consignee = 7,
    Depot = 8
}
