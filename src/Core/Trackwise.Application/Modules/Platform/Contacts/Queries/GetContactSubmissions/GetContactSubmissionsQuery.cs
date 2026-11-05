using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Contacts.Queries;

public sealed class GetContactSubmissionsQuery : SearchableQuery, IQuery<PagedResult<ContactSubmissionDto>>
{
}
