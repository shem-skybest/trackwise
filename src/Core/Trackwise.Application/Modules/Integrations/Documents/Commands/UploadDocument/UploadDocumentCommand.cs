using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Documents.Commands;

public class UploadDocumentCommand : ICommand<Result<Guid>>
{
    public DocumentOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }

    public required Stream FileContent { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DocumentType Type { get; set; }
    public string? Description { get; set; }
    public Guid UploadedById { get; set; }
}
