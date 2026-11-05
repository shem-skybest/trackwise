using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.TimeEntries.Queries;

public class GetTimeEntryByIdQuery : IQuery<Result<TimeEntryDto>>, IHaveId
{
    public required Guid Id { get; init; }
}
