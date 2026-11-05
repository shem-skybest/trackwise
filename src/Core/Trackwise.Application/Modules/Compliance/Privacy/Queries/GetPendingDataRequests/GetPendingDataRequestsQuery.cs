using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Queries;

/// <summary>
/// Admin-only - list pending exports and deletion requests across all tenants.
/// </summary>
public class GetPendingDataRequestsQuery : IQuery<Result<PendingDataRequestsDto>>
{
}

public record PendingDataRequestsDto
{
    public List<DataExportRequestDto> PendingExports { get; init; } = [];
    public List<DataDeletionRequestDto> PendingDeletions { get; init; } = [];
}
