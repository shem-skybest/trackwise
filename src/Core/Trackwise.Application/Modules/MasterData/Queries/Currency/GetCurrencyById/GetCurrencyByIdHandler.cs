using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a currency by id, following the merge chain when the id belongs to a record that has
/// since been folded into another.
/// </summary>
internal sealed class GetCurrencyByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetCurrencyByIdQuery, Result<CurrencyDto>>
{
    public async Task<Result<CurrencyDto>> Handle(
        GetCurrencyByIdQuery req, CancellationToken ct)
    {
        var currency = await tenantUow.Repository<Currency>().GetByIdAsync(req.Id, ct);

        if (currency is not null)
        {
            return Result<CurrencyDto>.Ok(currency.ToDto());
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Currency), req.Id, ct);

        if (!forwarded)
        {
            return Result<CurrencyDto>.Fail($"Could not find currency with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Currency>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<CurrencyDto>.Fail($"Could not find currency with ID '{req.Id}'");
        }

        var dto = survivor.ToDto();

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<CurrencyDto>.Ok(dto);
    }
}
