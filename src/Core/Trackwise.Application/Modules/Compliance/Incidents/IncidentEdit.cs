using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents;

/// <summary>Loads an incident, applies one change, saves and returns it - the shape of every incident command.</summary>
internal static class IncidentEdit
{
    /// <param name="change">Returns a failure to stop before saving, or null to save.</param>
    public static async Task<Result<IncidentDto>> ApplyAsync(
        ITenantUnitOfWork uow, Guid incidentId, Func<Incident, Task<Result?>> change, CancellationToken ct)
    {
        var incident = await uow.Repository<Incident>().GetByIdAsync(incidentId, ct);
        if (incident is null)
        {
            return Result<IncidentDto>.Fail($"Could not find the incident with ID '{incidentId}'.");
        }

        try
        {
            if (await change(incident) is { IsSuccess: false } failed)
            {
                return Result<IncidentDto>.Fail(failed.Error!);
            }

            await uow.SaveChangesAsync(ct);
            return Result<IncidentDto>.Ok(await IncidentReader.ToDetailsDtoAsync(uow, incident, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<IncidentDto>.Fail(ex.Message);
        }
    }

    public static Task<Result<IncidentDto>> ApplyAsync(
        ITenantUnitOfWork uow, Guid incidentId, Action<Incident> change, CancellationToken ct) =>
        ApplyAsync(uow, incidentId, incident =>
        {
            change(incident);
            return Task.FromResult<Result?>(null);
        }, ct);
}
