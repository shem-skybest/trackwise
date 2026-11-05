using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

public class GetRateAgreementsQuery : SearchableQuery, IQuery<PagedResult<RateAgreementDto>>
{
    public Guid? CustomerId { get; set; }
    public RateAgreementStatus? Status { get; set; }

    /// <summary>Only the agreements pricing trips on this date.</summary>
    public DateOnly? InForceOn { get; set; }
}
