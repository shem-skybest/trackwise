using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

/// <summary>Closes a fund once the office has reviewed every receipt.</summary>
public class LiquidateCashFundCommand : ICommand<Result<CashFundDto>>
{
    public Guid Id { get; set; }
}
