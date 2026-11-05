using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Queries;

internal sealed class GetEmployeeByIdHandler : IAppRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
{
    private readonly ITenantUnitOfWork _tenantUow;

    public GetEmployeeByIdHandler(ITenantUnitOfWork tenantUow)
    {
        _tenantUow = tenantUow;
    }

    public async Task<Result<EmployeeDto>> Handle(
        GetEmployeeByIdQuery req, CancellationToken ct)
    {
        var employeeEntity = await _tenantUow.Repository<Employee>().GetByIdAsync(req.UserId, ct);

        if (employeeEntity is null)
        {
            return Result<EmployeeDto>.Fail($"Could not find the specified employee with ID {req.UserId}");
        }

        var employeeDto = employeeEntity.ToDto();
        return Result<EmployeeDto>.Ok(employeeDto);
    }
}
