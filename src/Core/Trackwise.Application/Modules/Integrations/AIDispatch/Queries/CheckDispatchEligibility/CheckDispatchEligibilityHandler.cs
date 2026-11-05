using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Dispatch;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Queries;

internal sealed class CheckDispatchEligibilityHandler(IDispatchEligibilityService eligibilityService)
    : IAppRequestHandler<CheckDispatchEligibilityQuery, Result<EligibilityResultDto>>
{
    public async Task<Result<EligibilityResultDto>> Handle(
        CheckDispatchEligibilityQuery req, CancellationToken ct)
    {
        var result = await eligibilityService.CheckAsync(req.TruckId, req.LoadId, req.DriverId, ct);

        var dto = new EligibilityResultDto
        {
            IsEligible = result.IsEligible,
            Issues = result.Issues.Select(i => new EligibilityIssueDto
            {
                Code = i.Code.ToString(),
                Severity = i.Severity.ToString(),
                Message = i.Message
            }).ToList()
        };

        return Result<EligibilityResultDto>.Ok(dto);
    }
}
