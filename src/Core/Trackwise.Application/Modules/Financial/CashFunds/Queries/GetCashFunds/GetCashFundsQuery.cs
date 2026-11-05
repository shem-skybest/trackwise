using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Queries;

/// <summary>Funds newest period first, with their totals but not their lines.</summary>
public class GetCashFundsQuery : PagedQuery, IQuery<PagedResult<CashFundDto>>
{
    public CashFundStatus? Status { get; set; }
    public Guid? CustodianId { get; set; }

    /// <summary>Only funds whose period includes this day.</summary>
    public DateOnly? CoversDate { get; set; }
}
