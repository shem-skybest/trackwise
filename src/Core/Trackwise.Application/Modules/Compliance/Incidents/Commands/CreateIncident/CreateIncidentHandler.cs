using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class CreateIncidentHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateIncidentCommand, Result<IncidentDto>>
{
    public async Task<Result<IncidentDto>> Handle(CreateIncidentCommand req, CancellationToken ct)
    {
        var parties = await IncidentPartiesResolver.ResolveAsync(tenantUow, req, ct);
        if (!parties.IsSuccess)
        {
            return Result<IncidentDto>.Fail(parties.Error!);
        }

        // Allocated before the checks below, so a refused report leaves a gap - never a repeated number.
        var caseNumber = await codes.NextAsync(
            Incident.CaseNumberSeries, Incident.DefaultCaseNumberPrefix, Incident.CaseNumberPadWidth, ct);

        Incident incident;
        try
        {
            incident = Incident.Record(caseNumber, req.ToDetails(), parties.Value!, DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result<IncidentDto>.Fail(ex.Message);
        }

        await tenantUow.Repository<Incident>().AddAsync(incident, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<IncidentDto>.Ok(await IncidentReader.ToDetailsDtoAsync(tenantUow, incident, ct));
    }
}
