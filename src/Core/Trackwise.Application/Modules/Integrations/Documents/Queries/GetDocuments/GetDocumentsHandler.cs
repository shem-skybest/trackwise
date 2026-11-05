using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Integrations.Documents.Queries;

internal sealed class
    GetDocumentsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetDocumentsQuery, Result<IEnumerable<DocumentDto>>>
{
    public async Task<Result<IEnumerable<DocumentDto>>> Handle(
        GetDocumentsQuery req, CancellationToken ct)
    {
        // Verify the owner exists
        switch (req.OwnerType)
        {
            case DocumentOwnerType.Load when req.OwnerId is not null:
                if (await tenantUow.Repository<Load>().GetByIdAsync(req.OwnerId.Value, ct) is null)
                {
                    return Result<IEnumerable<DocumentDto>>.Fail($"Could not find load with ID '{req.OwnerId}'");
                }

                break;
            case DocumentOwnerType.Employee when req.OwnerId is not null:
                // Past the soft-delete filter: a retired employee's compliance documents (I-9s,
                // certifications) must stay reachable, not 404 the moment they're retired.
                var employeeExists = await tenantUow.Repository<Employee>().Query()
                    .IgnoreQueryFilters()
                    .AnyAsync(e => e.Id == req.OwnerId.Value, ct);

                if (!employeeExists)
                {
                    return Result<IEnumerable<DocumentDto>>.Fail($"Could not find employee with ID '{req.OwnerId}'");
                }

                break;
            case DocumentOwnerType.Truck when req.OwnerId is not null:
                // Past the soft-delete filter: a retired truck's compliance documents (insurance,
                // registration, inspection) must stay reachable, not 404 the moment it's retired.
                var truckExists = await tenantUow.Repository<Truck>().Query()
                    .IgnoreQueryFilters()
                    .AnyAsync(t => t.Id == req.OwnerId.Value, ct);

                if (!truckExists)
                {
                    return Result<IEnumerable<DocumentDto>>.Fail($"Could not find truck with ID '{req.OwnerId}'");
                }

                break;
        }


        // Fetch and filter by owner + optional status/type
        List<DocumentDto> dtos;
        if (req is { OwnerType: DocumentOwnerType.Load, OwnerId: not null })
        {
            List<LoadDocument> docs = await tenantUow.Repository<LoadDocument>()
                .GetListAsync(d =>
                    d.LoadId == req.OwnerId &&
                    (!req.Status.HasValue || d.Status == req.Status) &&
                    (!req.Type.HasValue || d.Type == req.Type), ct);

            dtos = docs.Select(d => d.ToDto()).ToList();
        }
        else if (req is { OwnerType: DocumentOwnerType.Employee, OwnerId: not null })
        {
            List<EmployeeDocument> docs = await tenantUow.Repository<EmployeeDocument>()
                .GetListAsync(d =>
                    d.EmployeeId == req.OwnerId &&
                    (!req.Status.HasValue || d.Status == req.Status) &&
                    (!req.Type.HasValue || d.Type == req.Type), ct);

            dtos = docs.Select(d => d.ToDto()).ToList();
        }
        else if (req is { OwnerType: DocumentOwnerType.Truck, OwnerId: not null })
        {
            List<TruckDocument> docs = await tenantUow.Repository<TruckDocument>()
                .GetListAsync(d =>
                    d.TruckId == req.OwnerId &&
                    (!req.Status.HasValue || d.Status == req.Status) &&
                    (!req.Type.HasValue || d.Type == req.Type), ct);

            dtos = docs.Select(d => d.ToDto()).ToList();
        }
        else
        {
            List<Document> docs = await tenantUow.Repository<Document>()
                .GetListAsync(d =>
                    (!req.Status.HasValue || d.Status == req.Status) &&
                    (!req.Type.HasValue || d.Type == req.Type), ct);

            dtos = docs.Select(d => d.ToDto()).ToList();
        }

        return Result<IEnumerable<DocumentDto>>.Ok(dtos);
    }
}
