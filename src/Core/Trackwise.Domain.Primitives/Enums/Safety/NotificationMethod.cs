using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums.Safety;

public enum NotificationMethod
{
    Push,

    [Description("SMS")]
    Sms,

    PhoneCall,
    Email
}
