namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RemoveQuotationLine;

using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

internal sealed class RemoveQuotationLineHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RemoveQuotationLineCommand, Result>
{
    public async Task<Result> Handle(RemoveQuotationLineCommand req, CancellationToken ct)
    {
        var quotationRepo = tenantUow.Repository<Quotation>();

        var quotation = await quotationRepo.GetByIdAsync(req.QuotationId, ct);

        if (quotation is null)
        {
            return Result.Fail($"Quotation with ID '{req.QuotationId}' not found.");
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return Result.Fail("Lines can only be removed from a Draft quotation.");
        }

        var lineExists = quotation.Lines.Any(l => l.Id == req.LineId);
        if (!lineExists)
        {
            return Result.Fail($"Quotation line with ID '{req.LineId}' not found on this quotation.");
        }

        quotation.RemoveLine(req.LineId);

        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
