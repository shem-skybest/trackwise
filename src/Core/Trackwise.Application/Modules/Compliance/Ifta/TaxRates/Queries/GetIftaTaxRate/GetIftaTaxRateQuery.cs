using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Ifta.TaxRates.Queries;

public sealed class GetIftaTaxRateQuery : IQuery<Result<IftaTaxRateDto>>
{
    public Guid Id { get; set; }
}
