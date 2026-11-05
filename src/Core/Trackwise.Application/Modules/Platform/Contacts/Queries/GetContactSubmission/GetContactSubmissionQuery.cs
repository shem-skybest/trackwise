using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Contacts.Queries;

public sealed class GetContactSubmissionQuery : IQuery<Result<ContactSubmissionDto>>
{
    public Guid Id { get; set; }
}
