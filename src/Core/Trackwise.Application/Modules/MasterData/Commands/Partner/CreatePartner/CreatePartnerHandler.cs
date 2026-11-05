using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreatePartnerHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreatePartnerCommand, Result<BusinessPartnerDto>>
{
    public async Task<Result<BusinessPartnerDto>> Handle(CreatePartnerCommand req, CancellationToken ct)
    {
        var code = BusinessPartner.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = BusinessPartner.NormalizeCode(
                await codes.NextAsync(nameof(BusinessPartner), "BP", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash with
            // a deleted partner is allowed, but the steward should be told what they are reusing
            // rather than silently colliding with history.
            var clash = await tenantUow.Repository<BusinessPartner>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Code == code, ct);

            if (clash is not null)
            {
                return Result<BusinessPartnerDto>.Fail(clash.DeletedAt is null
                    ? $"A business partner with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted partner. Restore that partner or choose another code.");
            }
        }

        if (req.CustomerId is { } customerId)
        {
            var customer = await tenantUow.Repository<Customer>().GetByIdAsync(customerId, ct);

            if (customer is null)
            {
                return Result<BusinessPartnerDto>.Fail($"Could not find customer with ID '{customerId}'");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<BusinessPartnerDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var partner = new BusinessPartner
        {
            Code = code,
            Name = req.Name,
            Roles = req.Roles.Distinct().ToList(),
            Address = req.Address,
            ContactName = req.ContactName,
            Email = req.Email,
            Phone = req.Phone,
            TaxId = req.TaxId,
            CustomerId = req.CustomerId,
            BranchId = req.BranchId,
            Notes = req.Notes,

            // Spelled out rather than left to the entity's defaults, because these two are the
            // policy rather than initial values: a partner is not live and nobody has looked at it.
            ApprovalState = MasterDataApprovalState.Draft,
            IsActive = false
        };

        await tenantUow.Repository<BusinessPartner>().AddAsync(partner, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<BusinessPartnerDto>.Ok(partner.ToDto());
    }
}
