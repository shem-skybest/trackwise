using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

public class DeleteCashFundExpenseCommand : ICommand<Result<CashFundDto>>
{
    public Guid CashFundId { get; set; }
    public Guid ExpenseId { get; set; }
}
