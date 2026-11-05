using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Queries;

public sealed class GetDemoRequestsQuery : SearchableQuery, IQuery<PagedResult<DemoRequestDto>>
{
}
