using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

public class ReopenCashFundCommand : ICommand<Result<CashFundDto>>
{
    public Guid Id { get; set; }
}
