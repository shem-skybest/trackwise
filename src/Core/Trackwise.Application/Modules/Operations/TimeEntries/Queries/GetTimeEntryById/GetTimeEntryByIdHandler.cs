using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.TimeEntries.Queries;

internal sealed class GetTimeEntryByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTimeEntryByIdQuery, Result<TimeEntryDto>>
{
    public async Task<Result<TimeEntryDto>> Handle(GetTimeEntryByIdQuery req, CancellationToken ct)
    {
        var entity = await tenantUow.Repository<TimeEntry>().GetByIdAsync(req.Id, ct);

        if (entity is null)
        {
            return Result<TimeEntryDto>.Fail($"Could not find a time entry with ID '{req.Id}'");
        }

        var employeeName = await EmployeeRefResolver.ResolveNameAsync(tenantUow, entity.EmployeeId, ct);

        return Result<TimeEntryDto>.Ok(entity.ToDto(employeeName));
    }
}
