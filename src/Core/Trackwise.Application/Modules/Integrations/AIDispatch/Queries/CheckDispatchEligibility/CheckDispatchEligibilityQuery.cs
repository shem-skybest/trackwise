using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Queries;

public class CheckDispatchEligibilityQuery : IQuery<Result<EligibilityResultDto>>
{
    public Guid TruckId { get; set; }
    public Guid LoadId { get; set; }
    public Guid? DriverId { get; set; }
}
