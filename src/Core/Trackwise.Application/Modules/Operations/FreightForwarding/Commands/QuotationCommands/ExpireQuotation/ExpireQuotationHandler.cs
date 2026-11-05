using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ExpireQuotation;

internal sealed class ExpireQuotationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ExpireQuotationCommand, Result>
{
    public async Task<Result> Handle(ExpireQuotationCommand req, CancellationToken ct)
    {
        var quotationRepo = tenantUow.Repository<Quotation>();
        var quotation = await quotationRepo.GetByIdAsync(req.Id, ct);

        if (quotation is null)
            return Result.Fail($"Quotation with ID '{req.Id}' not found.");

        if (quotation.Status != QuotationStatus.Issued)
            return Result.Fail($"Only Issued quotations can be expired. Current status: '{quotation.Status}'.");

        if (quotation.ValidUntil.HasValue && DateTimeOffset.UtcNow <= quotation.ValidUntil.Value)
        {
            return Result.Fail(
                $"Quotation has not yet reached its expiration date '{quotation.ValidUntil.Value:u}'.");
        }

        // Domain method sets Status to Expired and dispatches QuotationExpired event
        quotation.Expire();

        quotationRepo.Update(quotation);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
