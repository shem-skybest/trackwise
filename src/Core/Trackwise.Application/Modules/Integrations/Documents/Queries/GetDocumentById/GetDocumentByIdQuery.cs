using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Documents.Queries;

public class GetDocumentByIdQuery : IQuery<Result<DocumentDto>>
{
    public Guid DocumentId { get; set; }
}
