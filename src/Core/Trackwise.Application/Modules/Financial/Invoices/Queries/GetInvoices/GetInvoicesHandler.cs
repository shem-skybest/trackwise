using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Financial.Invoices.Specifications;
using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

internal sealed class GetInvoicesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetInvoicesQuery, PagedResult<InvoiceDto>>
{
    public Task<PagedResult<InvoiceDto>> Handle(GetInvoicesQuery req, CancellationToken ct)
    {
        // Determine which type of invoices to query
        if (req.InvoiceType == InvoiceType.Load)
        {
            return GetLoadInvoices(req);
        }else if(req.InvoiceType == InvoiceType.Booking)
        {
            return GetBookingInvoices(req);
        }

        if (req.EmployeeId.HasValue || !string.IsNullOrEmpty(req.EmployeeName) ||
            req.InvoiceType == InvoiceType.Payroll)
        {
            return GetPayrollInvoices(req, ct);
        }

        return GetAllInvoices(req, ct);
    }

    private async Task<PagedResult<InvoiceDto>> GetAllInvoices(GetInvoicesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Invoice>().Query();

        if (req.InvoiceType is not null)
        {
            query = query.Where(i => i.Type == req.InvoiceType);
        }

        var totalItems = query.Count();

        var invoices = query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToArray();

        // Every invoice type but payroll needs no employee - resolved past the filter only for the
        // rows that are actually PayrollInvoice, the same way GetPayrollInvoices does.
        var employees = await EmployeeRefResolver.ResolveEntitiesAsync(
            tenantUow,
            invoices.OfType<PayrollInvoice>().Select(p => p.EmployeeId).Distinct().ToArray(),
            ct);

        var invoicesDto = invoices
            .Select(i => i.ToDto(
                i is PayrollInvoice p && employees.TryGetValue(p.EmployeeId, out var e) ? e.ToDto() : null))
            .ToArray();

        return PagedResult<InvoiceDto>.Ok(invoicesDto, totalItems, req.PageSize);
    }

    private async Task<PagedResult<InvoiceDto>> GetLoadInvoices(GetInvoicesQuery req)
    {
        var specification = new FilterLoadInvoices(
            req.LoadId,
            req.Status,
            req.CustomerId,
            req.CustomerName,
            req.Search,
            req.StartDate,
            req.EndDate,
            req.OverdueOnly,
            req.OrderBy,
            req.Page,
            req.PageSize);

        var repository = tenantUow.Repository<LoadInvoice>();

        // Count with the same filter criteria (excluding pagination)
        var countSpec = new FilterLoadInvoices(
            req.LoadId,
            req.Status,
            req.CustomerId,
            req.CustomerName,
            req.Search,
            req.StartDate,
            req.EndDate,
            req.OverdueOnly,
            null,
            1,
            int.MaxValue);

        var totalItems = repository.ApplySpecification(countSpec).Count();

        var invoicesDto = repository
            .ApplySpecification(specification)
            .Select(i => i.ToDto())
            .ToArray();

        return PagedResult<InvoiceDto>.Ok(invoicesDto, totalItems, req.PageSize);
    }

    private async Task<PagedResult<InvoiceDto>> GetPayrollInvoices(GetInvoicesQuery req, CancellationToken ct)
    {
        // Resolved past the soft-delete filter: a plain i.Employee.FirstName criteria on the spec
        // below would join against Employee's own filter and silently drop every retired employee's
        // payroll history the moment either filter is used.
        IReadOnlyCollection<Guid>? matchingEmployeeIds = null;

        if (!string.IsNullOrWhiteSpace(req.EmployeeName) || req.SalaryType.HasValue)
        {
            var employeeQuery = tenantUow.Repository<Employee>().Query().IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(req.EmployeeName))
            {
                var lowerName = req.EmployeeName.ToLower();
                employeeQuery = employeeQuery.Where(e =>
                    e.FirstName.ToLower().Contains(lowerName) || e.LastName.ToLower().Contains(lowerName));
            }

            if (req.SalaryType.HasValue)
            {
                employeeQuery = employeeQuery.Where(e => e.SalaryType == req.SalaryType.Value);
            }

            matchingEmployeeIds = employeeQuery.Select(e => e.Id).ToArray();
        }

        var specification = new FilterPayrollInvoices(
            req.EmployeeId,
            req.EmployeeName,
            req.Status,
            req.SalaryType,
            req.StartDate,
            req.EndDate,
            matchingEmployeeIds,
            req.OrderBy,
            req.Page,
            req.PageSize);

        var repository = tenantUow.Repository<PayrollInvoice>();

        // Count with the same filter criteria (excluding pagination)
        var countSpec = new FilterPayrollInvoices(
            req.EmployeeId,
            req.EmployeeName,
            req.Status,
            req.SalaryType,
            req.StartDate,
            req.EndDate,
            matchingEmployeeIds,
            null,
            1,
            int.MaxValue);

        var totalItems = repository.ApplySpecification(countSpec).Count();

        var invoices = repository.ApplySpecification(specification).ToArray();

        var employees = await EmployeeRefResolver.ResolveEntitiesAsync(
            tenantUow, invoices.Select(p => p.EmployeeId).Distinct().ToArray(), ct);

        var invoicesDto = invoices
            .Select(i => i.ToDto(employees.TryGetValue(i.EmployeeId, out var e) ? e.ToDto() : null))
            .ToArray();

        return PagedResult<InvoiceDto>.Ok(invoicesDto, totalItems, req.PageSize);
    }

    private Task<PagedResult<InvoiceDto>> GetBookingInvoices(GetInvoicesQuery req)
    {
        var query = tenantUow.Repository<BookingInvoice>().Query();

        if (req.BookingId.HasValue)
        {
            query = query.Where(i => i.BookingId == req.BookingId.Value);
        }

        if (req.Statuses is { Count: > 0 })
        {
            query = query.Where(i => req.Statuses.Contains(i.Status));
        }

        else if (req.Status.HasValue)
        {
            query = query.Where(i => i.Status == req.Status.Value);
        }

        if (req.CustomerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == req.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.CustomerName))
        {
            var lowerName = req.CustomerName.ToLower();
            query = query.Where(i => i.Customer.Name.ToLower().Contains(lowerName));
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var lowerSearch = req.Search.ToLower();
            query = query.Where(i =>
                i.Number.ToString().Contains(lowerSearch) ||
                (i.Notes != null && i.Notes.ToLower().Contains(lowerSearch)) ||
                i.Customer.Name.ToLower().Contains(lowerSearch));
        }

        if (req.StartDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= req.StartDate.Value);
        }

        if (req.EndDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt <= req.EndDate.Value);
        }

        if (req.OverdueOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(i =>
                i.DueDate != null &&
                i.DueDate < now &&
                i.Status != InvoiceStatus.Paid);
        }

        // Count with the same filter criteria (excluding pagination)
        var totalItems = query.Count();

        var invoicesDto = query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .Select(i => i.ToDto())
            .ToArray();

        return Task.FromResult(PagedResult<InvoiceDto>.Ok(invoicesDto, totalItems, req.PageSize));
    }
}
