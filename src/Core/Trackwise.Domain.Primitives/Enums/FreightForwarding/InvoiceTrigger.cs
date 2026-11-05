using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding;

/// <summary>
/// When the customer is billed. Chosen at booking time (defaults from the customer / rate agreement)
/// and read by the Billing module when BookingConfirmed / trip events fire.
/// </summary>
public enum InvoiceTrigger
{
    OnConfirmation = 1,   // deposit or full prepay
    OnPickup = 2,
    OnDelivery = 3,       // on POD
    OnCompletion = 4      // single invoice once every trip is delivered
}
