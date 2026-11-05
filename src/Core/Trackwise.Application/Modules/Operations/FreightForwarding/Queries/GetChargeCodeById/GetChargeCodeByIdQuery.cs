// ─────────────────────────────────────────────
// GetChargeCodeByIdQuery.cs + Handler
// ─────────────────────────────────────────────
using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public class GetChargeCodeByIdQuery : IQuery<Result<ChargeCodeDto>>
{
    public Guid Id { get; set; }
}
