using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Queries;

public class GetCashFundByIdQuery : IQuery<Result<CashFundDto>>
{
    public Guid Id { get; set; }
}
