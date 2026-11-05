using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeCurrenciesHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeCurrenciesCommand, Result<CurrencyDto>>
{
    public Task<Result<CurrencyDto>> Handle(MergeCurrenciesCommand req, CancellationToken ct)
    {
        // The whole handler is the transactional unit, not just the merge: the repoint is
        // immediate SQL while the survivor's edited fields are only staged, so a failure
        // between them would move live references onto a record that was never written.
        // Loading inside the delegate is required too - a retry replays it against a
        // cleared change tracker.
        return tenantUow.ExecuteInTransactionAsync(
            async token =>
            {
                // Both sides are read past the query filter: merging away a record a steward already
                // deleted is a normal cleanup, and blocking it would leave the duplicate stranded.
                var records = await tenantUow.Repository<Currency>().Query()
                    .IgnoreQueryFilters()
                    .Where(c => c.Id == req.SurvivorId || c.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(c => c.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(c => c.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<CurrencyDto>.Fail(
                        $"Could not find the surviving currency '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<CurrencyDto>.Fail(
                        $"Could not find the currency to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup could
                // then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<CurrencyDto>.Fail(
                        $"Currency {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<CurrencyDto>.Fail(
                        $"Currency {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                // The ISO numeric code is the currency's IMO number: assigned once alongside the letters and
                // never shared. Two records carrying different ones are two currencies, and folding them
                // would silently re-denominate every amount the loser's code appears on.
                if (survivor.NumericCode is { } survivorNumeric
                    && loser.NumericCode is { } loserNumeric
                    && survivorNumeric != loserNumeric)
                {
                    return Result<CurrencyDto>.Fail(
                        $"{survivor.Code} is {survivorNumeric} and {loser.Code} is {loserNumeric}. Those are "
                        + "different currencies - clear one of the numeric codes if they really are one.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                await merges.MergeAsync(
                    nameof(Currency), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<CurrencyDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Currency survivor, Currency loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Currency.Name): survivor.Name = loser.Name; break;
                case nameof(Currency.Symbol): survivor.Symbol = loser.Symbol; break;
                case nameof(Currency.MinorUnits): survivor.MinorUnits = loser.MinorUnits; break;
                case nameof(Currency.Notes): survivor.Notes = loser.Notes; break;

                // Only reachable when the survivor had none - two different codes are refused
                // above - so this fills a gap rather than overwriting an assigned code.
                case nameof(Currency.NumericCode):
                    survivor.NumericCode = loser.NumericCode;
                    break;

                // Code is never taken from the loser: every Money in the schema holds it as text,
                // and the loser's own code keeps resolving through the cross-reference instead.
            }
        }
    }
}
