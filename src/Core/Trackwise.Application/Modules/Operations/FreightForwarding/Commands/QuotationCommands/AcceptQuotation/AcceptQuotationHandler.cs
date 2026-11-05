using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AcceptQuotation;

internal sealed class AcceptQuotationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<AcceptQuotationCommand, Result>
{
    public async Task<Result> Handle(AcceptQuotationCommand req, CancellationToken ct)
    {
        var quotationRepo = tenantUow.Repository<Quotation>();
        var quotation = await quotationRepo.GetByIdAsync(req.Id, ct);

        if (quotation is null)
            return Result.Fail($"Quotation with ID '{req.Id}' not found.");

        if (quotation.Status != QuotationStatus.Issued)
            return Result.Fail($"Only Issued quotations can be accepted. Current status: '{quotation.Status}'.");

        try
        {
            // Validates against ValidUntil TTL; raises QuotationAccepted domain event
            quotation.Accept();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        quotationRepo.Update(quotation);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
