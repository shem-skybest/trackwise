using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateLocationHandler(
    ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateLocationCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(CreateLocationCommand req, CancellationToken ct)
    {
        var code = Location.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Location.NormalizeCode(await codes.NextAsync(nameof(Location), "LOC", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted location is allowed, but the steward should be told what they are
            // reusing rather than silently colliding with history.
            var clash = await tenantUow.Repository<Location>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Code == code, ct);

            if (clash is not null)
            {
                return Result<LocationDto>.Fail(clash.DeletedAt is null
                    ? $"A location with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted location. Restore that location or choose another code.");
            }
        }

        if (req.ParentLocationId is { } parentId)
        {
            var parent = await tenantUow.Repository<Location>().GetByIdAsync(parentId, ct);

            if (parent is null)
            {
                return Result<LocationDto>.Fail($"Could not find zone with ID '{parentId}'");
            }

            // Only a zone groups other locations. Nesting a delivery point under a warehouse looks
            // harmless and then means nothing to anything reading the tree.
            if (parent.Type is not LocationType.Zone)
            {
                return Result<LocationDto>.Fail(
                    $"'{parent.Name}' is a {parent.Type}, not a zone. Only a zone can contain other locations.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<LocationDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        if (req.CustomerId is { } customerId
            && await tenantUow.Repository<Customer>().GetByIdAsync(customerId, ct) is null)
        {
            return Result<LocationDto>.Fail($"Could not find customer with ID '{customerId}'");
        }

        var location = new Location
        {
            Code = code,
            Name = req.Name,
            Type = req.Type,

            // A zone's address is dropped rather than stored. The validator already refuses one, so
            // this only catches a caller that skipped it - and a zone carrying one of its members'
            // addresses is how a zone quietly becomes a duplicate delivery point.
            Address = req.Type is LocationType.Zone ? null : req.Address,

            ParentLocationId = req.ParentLocationId,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            Notes = req.Notes,
            BranchId = req.BranchId,
            CustomerId = req.CustomerId,
            DefaultCutoffTime = req.DefaultCutoffTime,
            ZoneRadiusMeters = req.ZoneRadiusMeters
        };

        await tenantUow.Repository<Location>().AddAsync(location, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<LocationDto>.Ok(location.ToDto());
    }
}
