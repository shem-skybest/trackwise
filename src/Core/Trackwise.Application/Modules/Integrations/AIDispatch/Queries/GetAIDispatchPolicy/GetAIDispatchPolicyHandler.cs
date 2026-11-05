using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Queries;

internal sealed class GetAIDispatchPolicyHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetAIDispatchPolicyQuery, Result<AIDispatchPolicyDto>>
{
    public async Task<Result<AIDispatchPolicyDto>> Handle(
        GetAIDispatchPolicyQuery request, CancellationToken ct)
    {
        var policy = await tenantUow.Repository<AIDispatchPolicy>().Query().FirstOrDefaultAsync(ct);

        // No row is normal for a new tenant. A blank enabled policy keeps the null branch out of the
        // controller, the generated client and the page.
        return Result<AIDispatchPolicyDto>.Ok(policy?.ToDto() ?? AIDispatchPolicyDto.Empty);
    }
}
