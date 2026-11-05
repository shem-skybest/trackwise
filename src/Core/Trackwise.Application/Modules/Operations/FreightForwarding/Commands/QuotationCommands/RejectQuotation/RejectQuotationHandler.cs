using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RejectQuotation;

internal sealed class RejectQuotationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RejectQuotationCommand, Result>
{
    public async Task<Result> Handle(RejectQuotationCommand req, CancellationToken ct)
    {
        var quotationRepo = tenantUow.Repository<Quotation>();
        var quotation = await quotationRepo.GetByIdAsync(req.Id, ct);

        if (quotation is null)
            return Result.Fail($"Quotation with ID '{req.Id}' not found.");

        // Allows rejecting internally during review, or after being presented to the client
        if (quotation.Status != QuotationStatus.Draft &&
            quotation.Status != QuotationStatus.PendingApproval &&
            quotation.Status != QuotationStatus.Issued)
        {
            return Result.Fail($"Quotation in status '{quotation.Status}' cannot be rejected.");
        }

        quotation.Reject(req.RejectedBy, req.Reason);

        quotationRepo.Update(quotation);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
