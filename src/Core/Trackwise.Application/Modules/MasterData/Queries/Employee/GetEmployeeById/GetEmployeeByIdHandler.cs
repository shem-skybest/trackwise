using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads an employee by id, following the merge chain when the id belongs to a record that has
/// since been folded into another.
/// </summary>
internal sealed class GetEmployeeByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery req, CancellationToken ct)
    {
        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(req.Id, ct);

        if (employee is not null)
        {
            return Result<EmployeeDto>.Ok(await ToDtoAsync(employee, ct));
        }

        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Employee), req.Id, ct);

        if (!forwarded)
        {
            return Result<EmployeeDto>.Fail($"Could not find employee with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Employee>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<EmployeeDto>.Fail($"Could not find employee with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);
        dto.ForwardedFromId = req.Id;

        return Result<EmployeeDto>.Ok(dto);
    }

    private async Task<EmployeeDto> ToDtoAsync(Employee employee, CancellationToken ct) =>
        employee.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [employee.BranchId], ct));
}
