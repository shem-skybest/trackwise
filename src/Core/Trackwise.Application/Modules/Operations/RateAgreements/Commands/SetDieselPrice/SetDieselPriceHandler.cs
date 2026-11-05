using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class SetDieselPriceHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SetDieselPriceCommand, Result<DieselPriceDto>>
{
    public async Task<Result<DieselPriceDto>> Handle(SetDieselPriceCommand req, CancellationToken ct)
    {
        var repository = tenantUow.Repository<DieselPrice>();
        var entry = await repository.Query().FirstOrDefaultAsync(d => d.EffectiveFrom == req.EffectiveFrom, ct);

        if (entry is null)
        {
            entry = new DieselPrice { EffectiveFrom = req.EffectiveFrom };
            await repository.AddAsync(entry, ct);
        }

        entry.PricePerLitre = req.PricePerLitre;
        entry.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();

        await tenantUow.SaveChangesAsync(ct);
        return Result<DieselPriceDto>.Ok(entry.ToDto());
    }
}
