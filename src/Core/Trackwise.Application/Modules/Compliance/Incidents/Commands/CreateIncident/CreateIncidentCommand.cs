using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

public class CreateIncidentCommand : IncidentFields, ICommand<Result<IncidentDto>>;
