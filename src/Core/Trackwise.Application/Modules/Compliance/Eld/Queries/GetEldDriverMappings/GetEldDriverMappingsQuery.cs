using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Eld.Queries;

public class GetEldDriverMappingsQuery : IQuery<Result<List<EldDriverMappingDto>>>
{
    public Guid ProviderId { get; set; }
}
