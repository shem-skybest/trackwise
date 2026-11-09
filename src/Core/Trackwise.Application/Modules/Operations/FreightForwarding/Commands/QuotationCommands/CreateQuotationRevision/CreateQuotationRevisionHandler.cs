using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotationRevision;

internal sealed class CreateQuotationRevisionHandler(
    ITenantUnitOfWork tenantUow,
    IQuotationReferenceGenerator referenceGenerator)
    : IAppRequestHandler<CreateQuotationRevisionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateQuotationRevisionCommand request, CancellationToken ct)
    {
        var quotationRepo = tenantUow.Repository<Quotation>();
        var source = await quotationRepo.Query()
            .Include(quotation => quotation.Lines)
            .Include(quotation => quotation.Branch)
            .FirstOrDefaultAsync(quotation => quotation.Id == request.Id, ct);

        if (source is null)
            return Result<Guid>.Fail($"Quotation '{request.Id}' not found.", ErrorCodes.NotFound);

        if (source.Status == QuotationStatus.Draft)
            return Result<Guid>.Fail(
                "A draft quotation can be edited directly and cannot be revised.",
                ErrorCodes.Conflict);

        if (await quotationRepo.Query().AnyAsync(quotation => quotation.PreviousRevisionId == source.Id, ct))
            return Result<Guid>.Fail(
                "Only the latest quotation revision can be revised.",
                ErrorCodes.Conflict);

        var latestRevisionNumber = await quotationRepo.Query()
            .Where(quotation => quotation.RevisionGroupId == source.RevisionGroupId)
            .MaxAsync(quotation => (int?)quotation.RevisionNumber, ct);
        var nextRevisionNumber = Math.Max(source.RevisionNumber, latestRevisionNumber ?? source.RevisionNumber) + 1;
        var referenceNumber = await referenceGenerator.NextAsync(
            source.FreightMode,
            source.Branch.Code,
            ct);

        Quotation revision;
        try
        {
            revision = source.CreateRevision(referenceNumber, nextRevisionNumber);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Guid>.Fail(ex.Message);
        }

        await quotationRepo.AddAsync(revision, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(revision.Id);
    }
}
