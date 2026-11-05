using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

/// <summary>Diesel price history, newest first.</summary>
public class GetDieselPricesQuery : PagedQuery, IQuery<PagedResult<DieselPriceDto>>;
