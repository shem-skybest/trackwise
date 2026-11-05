using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Tax.Queries;

public record GetTenantTaxRatesQuery : IQuery<Result<IReadOnlyList<TenantTaxRateDto>>>;
