using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>
/// Switches whether an agreement bills extra drops and whether its amounts include VAT, on a draft
/// or an approved card alike. Trips already priced keep their price until priced again.
/// </summary>
public class SetRateAgreementBillingRulesCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
    public bool BillExtraDrops { get; set; }
    public bool RatesIncludeVat { get; set; }
}
