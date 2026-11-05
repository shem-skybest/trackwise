using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

internal sealed class GetInvoiceByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByIdQuery req, CancellationToken ct)
    {
        var entity = await tenantUow.Repository<Invoice>().GetByIdAsync(req.Id, ct);

        if (entity is null)
        {
            return Result<InvoiceDto>.Fail($"Could not find a invoice with ID '{req.Id}'");
        }

        EmployeeDto? payrollEmployee = null;

        if (entity is PayrollInvoice payrollInvoice)
        {
            var employees = await EmployeeRefResolver.ResolveEntitiesAsync(
                tenantUow, [payrollInvoice.EmployeeId], ct);

            payrollEmployee = employees.TryGetValue(payrollInvoice.EmployeeId, out var e) ? e.ToDto() : null;
        }

        return Result<InvoiceDto>.Ok(entity.ToDto(payrollEmployee));
    }
}
