using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

/// <summary>Corrects an expense whose receipt is still with Ops.</summary>
public class UpdateCashFundExpenseCommand : CashExpenseCommand, ICommand<Result<CashFundDto>>
{
    public Guid ExpenseId { get; set; }
}
