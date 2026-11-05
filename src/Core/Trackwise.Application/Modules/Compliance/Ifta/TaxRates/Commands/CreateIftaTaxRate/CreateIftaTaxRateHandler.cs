using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Compliance.Ifta.TaxRates.Commands;

internal sealed class CreateIftaTaxRateHandler(
    IMasterUnitOfWork masterUow,
    ILogger<CreateIftaTaxRateHandler> logger) : IAppRequestHandler<CreateIftaTaxRateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateIftaTaxRateCommand req, CancellationToken ct)
    {
        var jurisdiction = TaxJurisdiction.Create(req.CountryCode, req.Region);

        var conflict = await IftaTaxRateUniqueness.FindConflictAsync(
            masterUow, jurisdiction, req.Year, req.Quarter, excludeId: null, ct);

        if (conflict is not null)
        {
            return Result<Guid>.Fail(conflict);
        }

        var rate = new IftaTaxRate
        {
            Jurisdiction = jurisdiction,
            Year = req.Year,
            Quarter = req.Quarter,
            RatePerGallon = req.RatePerGallon,
            SurchargeRatePerGallon = req.SurchargeRatePerGallon,
        };

        await masterUow.Repository<IftaTaxRate>().AddAsync(rate, ct);
        await masterUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created IFTA tax rate {Jurisdiction} {Year} Q{Quarter}", rate.Jurisdiction, req.Year, req.Quarter);
        return Result<Guid>.Ok(rate.Id);
    }
}
