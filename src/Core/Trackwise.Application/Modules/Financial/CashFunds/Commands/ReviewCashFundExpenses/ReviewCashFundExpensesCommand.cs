using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

/// <summary>The office checks forwarded receipts, or disallows them with a reason.</summary>
public class ReviewCashFundExpensesCommand : ICommand<Result<CashFundDto>>
{
    public Guid Id { get; set; }
    public List<Guid> ExpenseIds { get; set; } = [];

    /// <summary>True accepts the receipts; false disallows them and needs <see cref="Reason" />.</summary>
    public bool Accept { get; set; }

    public string? Reason { get; set; }
}
