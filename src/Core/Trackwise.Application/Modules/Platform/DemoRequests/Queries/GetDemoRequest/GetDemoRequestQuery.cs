using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Queries;

public sealed class GetDemoRequestQuery : IQuery<Result<DemoRequestDto>>
{
    public Guid Id { get; set; }
}
