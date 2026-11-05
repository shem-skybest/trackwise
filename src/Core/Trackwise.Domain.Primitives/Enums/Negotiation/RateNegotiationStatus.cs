namespace Trackwise.Domain.Primitives.Enums;

public enum RateNegotiationStatus
{
    AwaitingBroker,
    BrokerReplied,
    Accepted,
    Declined,
    Expired,
    Closed
}
