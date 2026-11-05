using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

/// <summary>What a fund's expenses add up to, per receipt status - one grouped query for a whole list page.</summary>
public sealed record CashExpenseTally(CashExpenseStatus Status, int Count, decimal Amount);

[Mapper]
public static partial class CashFundMapper
{
    /// <summary>Header only. The totals are the entity's own, but reading them loads every line, so they're set from a tally.</summary>
    [MapperIgnoreSource(nameof(CashFund.DomainEvents))]
    [MapperIgnoreSource(nameof(CashFund.Custodian))]
    [MapperIgnoreSource(nameof(CashFund.Expenses))]
    [MapperIgnoreSource(nameof(CashFund.SpentTotal))]
    [MapperIgnoreSource(nameof(CashFund.DisallowedTotal))]
    [MapperIgnoreSource(nameof(CashFund.Balance))]
    [MapperIgnoreSource(nameof(CashFund.UpdatedAt))]
    [MapperIgnoreSource(nameof(CashFund.UpdatedBy))]
    [MapperIgnoreTarget(nameof(CashFundDto.CustodianName))]
    [MapperIgnoreTarget(nameof(CashFundDto.SpentTotal))]
    [MapperIgnoreTarget(nameof(CashFundDto.DisallowedTotal))]
    [MapperIgnoreTarget(nameof(CashFundDto.Balance))]
    [MapperIgnoreTarget(nameof(CashFundDto.ExpenseCount))]
    [MapperIgnoreTarget(nameof(CashFundDto.WithOpsCount))]
    [MapperIgnoreTarget(nameof(CashFundDto.ForwardedCount))]
    [MapperIgnoreTarget(nameof(CashFundDto.Expenses))]
    private static partial CashFundDto ToHeaderDto(this CashFund entity);

    public static CashFundDto ToDto(this CashFund entity, string? custodianName, IReadOnlyCollection<CashExpenseTally> tally)
    {
        var spent = tally.Where(t => t.Status != CashExpenseStatus.Disallowed).Sum(t => t.Amount);
        return entity.ToHeaderDto() with
        {
            CustodianName = custodianName,
            SpentTotal = spent,
            DisallowedTotal = tally.Where(t => t.Status == CashExpenseStatus.Disallowed).Sum(t => t.Amount),
            Balance = entity.Amount - spent,
            ExpenseCount = tally.Sum(t => t.Count),
            WithOpsCount = tally.Where(t => t.Status == CashExpenseStatus.WithOps).Sum(t => t.Count),
            ForwardedCount = tally.Where(t => t.Status == CashExpenseStatus.Forwarded).Sum(t => t.Count)
        };
    }

    /// <summary>With its expenses, already mapped with their names by the caller.</summary>
    public static CashFundDto ToDto(this CashFund entity, string? custodianName, IReadOnlyList<CashFundExpenseDto> expenses)
    {
        var tally = expenses
            .GroupBy(e => e.Status)
            .Select(g => new CashExpenseTally(g.Key, g.Count(), g.Sum(e => e.Amount)))
            .ToList();

        return entity.ToDto(custodianName, tally) with { Expenses = expenses };
    }

    [MapperIgnoreSource(nameof(CashFundExpense.DomainEvents))]
    [MapperIgnoreSource(nameof(CashFundExpense.CashFundId))]
    [MapperIgnoreSource(nameof(CashFundExpense.Truck))]
    [MapperIgnoreSource(nameof(CashFundExpense.Trip))]
    [MapperIgnoreSource(nameof(CashFundExpense.SpentBy))]
    [MapperIgnoreSource(nameof(CashFundExpense.CountsAgainstFund))]
    [MapperIgnoreSource(nameof(CashFundExpense.CreatedAt))]
    [MapperIgnoreSource(nameof(CashFundExpense.CreatedBy))]
    [MapperIgnoreSource(nameof(CashFundExpense.UpdatedAt))]
    [MapperIgnoreSource(nameof(CashFundExpense.UpdatedBy))]
    [MapperIgnoreTarget(nameof(CashFundExpenseDto.TruckNumber))]
    [MapperIgnoreTarget(nameof(CashFundExpenseDto.TripNumber))]
    [MapperIgnoreTarget(nameof(CashFundExpenseDto.SpentByName))]
    public static partial CashFundExpenseDto ToDto(this CashFundExpense entity);
}
