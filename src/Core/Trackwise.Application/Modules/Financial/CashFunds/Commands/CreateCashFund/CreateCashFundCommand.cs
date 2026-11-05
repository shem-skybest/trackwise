using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

public class CreateCashFundCommand : CashFundTermsCommand, ICommand<Result<CashFundDto>>;
