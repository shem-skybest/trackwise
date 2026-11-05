using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Queries;

public record GetIncidentByIdQuery(Guid Id) : IQuery<Result<IncidentDto>>, IHaveId;
