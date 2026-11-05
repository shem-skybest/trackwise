using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Ifta.TaxRates.Queries;

public sealed class GetIftaTaxRatesQuery : SearchableQuery, IQuery<PagedResult<IftaTaxRateDto>>
{
    public int? Year { get; set; }
    public int? Quarter { get; set; }
    public string? CountryCode { get; set; }
}
