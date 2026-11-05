using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Eld.Queries;

public class GetEldProviderDriversQuery : IQuery<Result<List<EldDriverDto>>>
{
    public Guid ProviderId { get; set; }
}
