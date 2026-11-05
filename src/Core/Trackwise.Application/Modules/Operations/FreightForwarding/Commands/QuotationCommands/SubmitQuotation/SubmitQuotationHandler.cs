using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.SubmitQuotation;

internal sealed class SubmitQuotationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SubmitQuotationCommand, Result>
{
    public async Task<Result> Handle(SubmitQuotationCommand req, CancellationToken ct)
    {
        var quotationRepo = tenantUow.Repository<Quotation>();
        var quotation = await quotationRepo.GetByIdAsync(req.Id, ct);

        if (quotation is null)
            return Result.Fail($"Quotation with ID '{req.Id}' not found.");

        if (quotation.Status != QuotationStatus.Draft)
            return Result.Fail($"Only Draft quotations can be submitted. Current status: '{quotation.Status}'.");

        if (quotation.Lines.Count == 0)
            return Result.Fail("Cannot submit a quotation without priced line items.");

        try
        {
            quotation.Submit();
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
