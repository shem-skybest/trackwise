// ─────────────────────────────────────────────
// GetTariffByIdQuery.cs
// ─────────────────────────────────────────────
using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public class GetTariffByIdQuery : IQuery<Result<TariffDto>>
{
    public Guid Id { get; set; }
}