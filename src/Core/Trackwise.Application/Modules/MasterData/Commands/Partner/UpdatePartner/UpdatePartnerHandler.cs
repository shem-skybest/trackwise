using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdatePartnerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdatePartnerCommand, Result>
{
    public async Task<Result> Handle(UpdatePartnerCommand req, CancellationToken ct)
    {
        var partner = await tenantUow.Repository<BusinessPartner>().GetByIdAsync(req.Id, ct);

        if (partner is null)
        {
            return Result.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        // The invariant IMasterDataApprovable exists to protect. Without this, "active" is just a
        // field on the edit form and the approval step is a formality anybody can route around.
        if (req.IsActive && partner.ApprovalState is not MasterDataApprovalState.Approved)
        {
            return Result.Fail(
                $"{partner.Code} has not been approved yet, so it cannot be made active here. "
                + "Submit it for approval instead.");
        }

        if (req.CustomerId is { } customerId && customerId != partner.CustomerId)
        {
            var customer = await tenantUow.Repository<Customer>().GetByIdAsync(customerId, ct);

            if (customer is null)
            {
                return Result.Fail($"Could not find customer with ID '{customerId}'");
            }
        }

        if (req.BranchId is { } branchId && branchId != partner.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        partner.Name = req.Name;
        partner.Roles = req.Roles.Distinct().ToList();
        partner.Address = req.Address;
        partner.ContactName = req.ContactName;
        partner.Email = req.Email;
        partner.Phone = req.Phone;
        partner.TaxId = req.TaxId;
        partner.CustomerId = req.CustomerId;
        partner.BranchId = req.BranchId;
        partner.Notes = req.Notes;
        partner.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
