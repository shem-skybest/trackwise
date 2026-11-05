using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

/// <summary>Ops sends receipts to the office, dated today.</summary>
public class ForwardCashFundExpensesCommand : ICommand<Result<CashFundDto>>
{
    public Guid Id { get; set; }

    /// <summary>The receipts to send; every receipt still with Ops when empty.</summary>
    public List<Guid> ExpenseIds { get; set; } = [];
}
