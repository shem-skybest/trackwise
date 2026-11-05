using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Tracking.Queries;

public class DownloadPublicTrackingDocumentQuery : IQuery<Result<DocumentDownloadDto>>
{
    public required Guid TenantId { get; init; }
    public required string Token { get; init; }
    public required Guid DocumentId { get; init; }
}
