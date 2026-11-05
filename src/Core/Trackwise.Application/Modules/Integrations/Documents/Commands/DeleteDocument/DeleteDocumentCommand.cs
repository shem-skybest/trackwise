using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Integrations.Documents.Commands;

public class DeleteDocumentCommand : ICommand
{
    public Guid DocumentId { get; set; }
}
