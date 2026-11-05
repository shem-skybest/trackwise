using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>The fuel added to a rate line when diesel is at or above <see cref="DieselPrice"/>.</summary>
public class RateFuelSurcharge : Entity, ITenantEntity
{
    public Guid RateAgreementLineId { get; set; }

    /// <summary>The diesel price per litre this tier starts at, e.g. 85.00.</summary>
    public decimal DieselPrice { get; set; }

    public decimal Amount { get; set; }
}
