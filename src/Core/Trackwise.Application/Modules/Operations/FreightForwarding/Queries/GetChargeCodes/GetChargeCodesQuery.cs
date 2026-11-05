using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public class GetChargeCodesQuery : IQuery<Result<IEnumerable<ChargeCodeDto>>>
{
    public bool OnlyActive { get; set; } = true;
}
