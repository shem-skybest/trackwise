using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ApproveQuotation;

internal sealed class ApproveQuotationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ApproveQuotationCommand, Result>
{
    public async Task<Result> Handle(ApproveQuotationCommand req, CancellationToken ct)
    {
        var quotation = await tenantUow.Repository<Quotation>().GetByIdAsync(req.Id, ct);
        if (quotation is null)
        {
            return Result.Fail($"Quotation with ID '{req.Id}' not found.");
        }

        // Support approving both escalated quotes and direct draft reviews
        if (quotation.Status != QuotationStatus.PendingApproval && quotation.Status != QuotationStatus.Draft)
        {
            return Result.Fail($"Only Draft or PendingApproval quotations can be approved. Current status: '{quotation.Status}'.");
        }

        if (quotation.Lines.Count == 0)
        {
            return Result.Fail("Cannot approve a quotation with no line items.");
        }

        quotation.Approve(req.ApprovedBy);

        tenantUow.Repository<Quotation>().Update(quotation);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
