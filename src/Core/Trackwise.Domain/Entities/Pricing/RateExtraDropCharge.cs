using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// What each drop past the ones a trip's rate includes costs, for one truck size - the quotes'
/// "plus ₱750.00 (2tons) &amp; ₱1,500.00 (4tons) per drop".
/// </summary>
public class RateExtraDropCharge : Entity, ITenantEntity
{
    public Guid RateAgreementId { get; set; }

    /// <summary>The truck size this charge is for, or null when one charge covers every size.</summary>
    public TruckTonnageClass? Tonnage { get; set; }

    public decimal Amount { get; set; }
}
