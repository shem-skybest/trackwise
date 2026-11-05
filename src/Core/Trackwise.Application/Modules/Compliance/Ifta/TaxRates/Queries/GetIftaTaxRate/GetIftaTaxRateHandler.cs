using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Ifta.TaxRates.Queries;

internal sealed class GetIftaTaxRateHandler(
    IMasterUnitOfWork masterUow) : IAppRequestHandler<GetIftaTaxRateQuery, Result<IftaTaxRateDto>>
{
    public async Task<Result<IftaTaxRateDto>> Handle(GetIftaTaxRateQuery req, CancellationToken ct)
    {
        var rate = await masterUow.Repository<IftaTaxRate>().GetByIdAsync(req.Id, ct);

        if (rate is null)
        {
            return Result<IftaTaxRateDto>.Fail($"IFTA tax rate with ID '{req.Id}' not found");
        }

        return Result<IftaTaxRateDto>.Ok(rate.ToDto());
    }
}
