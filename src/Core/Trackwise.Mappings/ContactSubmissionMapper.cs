using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class ContactSubmissionMapper
{
    [MapperIgnoreSource(nameof(ContactSubmission.DomainEvents))]
    public static partial ContactSubmissionDto ToDto(this ContactSubmission entity);
}
