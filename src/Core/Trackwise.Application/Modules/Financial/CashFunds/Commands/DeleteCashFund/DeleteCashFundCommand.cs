using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

/// <summary>Deletes a fund entered by mistake - only while none of its receipts has reached the office.</summary>
public class DeleteCashFundCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
