using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.Documents.Commands;

public class UpdateDocumentCommand : ICommand
{
    public Guid DocumentId { get; set; }
    public DocumentType? Type { get; set; }
    public string? Description { get; set; }
    public Guid UpdatedById { get; set; }
}
