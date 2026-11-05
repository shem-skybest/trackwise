using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Application.Modules.Operations;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Compliance.Incidents;

/// <summary>Incidents with their trip, client, truck and crew named in one batch, for lists and the details page.</summary>
internal static class IncidentReader
{
    public static async Task<IReadOnlyList<IncidentDto>> ToDtosAsync(
        ITenantUnitOfWork uow, IReadOnlyCollection<Incident> incidents, CancellationToken ct)
    {
        if (incidents.Count == 0)
        {
            return [];
        }

        var tripIds = incidents.Select(i => i.TripId).OfType<Guid>().Distinct().ToList();
        var tripNumbers = tripIds.Count == 0
            ? new Dictionary<Guid, long>()
            : await uow.Repository<Trip>().Query()
                .Where(t => tripIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Number, ct);

        var customerIds = incidents.Select(i => i.CustomerId).OfType<Guid>().Distinct().ToList();
        var customerNames = customerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await uow.Repository<Customer>().Query()
                .IgnoreQueryFilters()
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var trucks = await TruckRefResolver.ResolveAsync(
            uow, incidents.Select(i => i.TruckId).OfType<Guid>().Distinct().ToList(), ct);
        var drivers = await EmployeeRefResolver.ResolveAsync(
            uow, incidents.Select(i => i.DriverId).OfType<Guid>().Distinct().ToList(), ct);
        var helpers = await HelperRefResolver.ResolveAsync(
            uow,
            incidents.SelectMany(i => new[] { i.HelperId, i.SecondHelperId }).OfType<Guid>().Distinct().ToList(),
            ct);

        return incidents
            .Select(i => i.ToDto(new IncidentRefNames(
                i.TripId is { } t && tripNumbers.TryGetValue(t, out var number) ? number : null,
                i.CustomerId is { } c && customerNames.TryGetValue(c, out var customer) ? customer : null,
                i.TruckId is { } tr && trucks.TryGetValue(tr, out var truck) ? truck.Number : null,
                i.DriverId is { } d && drivers.TryGetValue(d, out var driver) ? driver.Name : null,
                i.HelperId is { } h && helpers.TryGetValue(h, out var helper) ? helper : null,
                i.SecondHelperId is { } h2 && helpers.TryGetValue(h2, out var second) ? second : null)))
            .ToList();
    }

    /// <summary>One incident with its linked repairs and the rescues it called for.</summary>
    public static async Task<IncidentDto> ToDetailsDtoAsync(ITenantUnitOfWork uow, Incident incident, CancellationToken ct)
    {
        var names = (await ToDtosAsync(uow, [incident], ct))[0];

        var repairs = (await uow.Repository<MaintenanceRecord>().Query()
                .Where(r => r.IncidentId == incident.Id)
                .OrderBy(r => r.ServiceDate)
                .ToListAsync(ct))
            .Select(r => r.ToIncidentRepairDto())
            .ToList();
        var rescues = await TripRescueReader.ForIncidentAsync(uow, incident.Id, ct);
        var attachments = await AttachmentsAsync(uow, incident.Id, ct);

        return incident.ToDto(
            new IncidentRefNames(
                names.TripNumber, names.CustomerName, names.TruckNumber,
                names.DriverName, names.HelperName, names.SecondHelperName),
            repairs,
            rescues,
            attachments);
    }

    private static async Task<IReadOnlyList<IncidentAttachmentDto>> AttachmentsAsync(
        ITenantUnitOfWork uow, Guid incidentId, CancellationToken ct)
    {
        var documents = await uow.Repository<IncidentDocument>().Query()
            .Where(d => d.IncidentId == incidentId && d.Status == DocumentStatus.Active)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);
        var uploaders = await EmployeeRefResolver.ResolveAsync(
            uow, documents.Select(d => d.UploadedById).Distinct().ToList(), ct);

        return documents
            .Select(d => d.ToIncidentAttachmentDto(
                uploaders.TryGetValue(d.UploadedById, out var uploader) ? uploader.Name : null))
            .ToList();
    }
}
