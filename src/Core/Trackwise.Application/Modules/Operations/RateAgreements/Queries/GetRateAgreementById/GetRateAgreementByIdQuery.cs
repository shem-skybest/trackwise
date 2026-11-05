using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

public class GetRateAgreementByIdQuery : IQuery<Result<RateAgreementDto>>
{
    public Guid Id { get; set; }
}
