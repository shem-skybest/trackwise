using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Documents.Queries;

public class DownloadDocumentQuery : IQuery<Result<DocumentDownloadDto>>
{
    public Guid DocumentId { get; set; }
    public Guid RequestedById { get; set; }
}
