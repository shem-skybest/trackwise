using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.LoadBoard.Queries;

public class GetPostedTrucksQuery : IQuery<Result<List<PostedTruckDto>>>
{
    public LoadBoardProviderType? ProviderType { get; set; }
    public PostedTruckStatus? Status { get; set; }
    public Guid? TruckId { get; set; }
}
