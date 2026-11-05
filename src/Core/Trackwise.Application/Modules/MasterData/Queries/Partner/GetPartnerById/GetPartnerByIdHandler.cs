using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a business partner by id, following the merge chain when the id belongs to a record that
/// has since been folded into another.
/// </summary>
internal sealed class GetPartnerByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetPartnerByIdQuery, Result<BusinessPartnerDto>>
{
    public async Task<Result<BusinessPartnerDto>> Handle(
        GetPartnerByIdQuery req, CancellationToken ct)
    {
        var partner = await tenantUow.Repository<BusinessPartner>().GetByIdAsync(req.Id, ct);

        if (partner is not null)
        {
            return Result<BusinessPartnerDto>.Ok(await ToDtoAsync(partner, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(BusinessPartner), req.Id, ct);

        if (!forwarded)
        {
            return Result<BusinessPartnerDto>.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<BusinessPartner>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<BusinessPartnerDto>.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<BusinessPartnerDto>.Ok(dto);
    }

    private async Task<BusinessPartnerDto> ToDtoAsync(BusinessPartner partner, CancellationToken ct)
    {
        var dto = partner.ToDto(
            await BranchNameLookup.ResolveAsync(tenantUow, [partner.BranchId], ct));

        MasterDataApprovalLookup.Apply(
            dto,
            await MasterDataApprovalLookup.ResolveAsync(
                tenantUow, nameof(BusinessPartner), [partner.Id], ct));

        return dto;
    }
}
