using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateLocationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateLocationCommand, Result>
{
    public async Task<Result> Handle(UpdateLocationCommand req, CancellationToken ct)
    {
        var location = await tenantUow.Repository<Location>().GetByIdAsync(req.Id, ct);
        if (location is null)
        {
            return Result.Fail($"Could not find location with ID '{req.Id}'");
        }

        // Demoting a zone that still contains locations would leave them nested under something
        // that is no longer a grouping. The dependency probe only fires on a retirement, so this
        // transition needs its own check.
        if (location.Type is LocationType.Zone && req.Type is not LocationType.Zone)
        {
            var contained = await tenantUow.Repository<Location>().Query()
                .CountAsync(l => l.ParentLocationId == location.Id, ct);

            if (contained > 0)
            {
                return Result.Fail(
                    $"'{location.Name}' still contains {contained} location(s). Move them to "
                    + "another zone before changing what this record is.");
            }
        }

        if (req.ParentLocationId is { } parentId && parentId != location.ParentLocationId)
        {
            var parent = await tenantUow.Repository<Location>().GetByIdAsync(parentId, ct);

            if (parent is null)
            {
                return Result.Fail($"Could not find zone with ID '{parentId}'");
            }

            if (parent.Type is not LocationType.Zone)
            {
                return Result.Fail(
                    $"'{parent.Name}' is a {parent.Type}, not a zone. Only a zone can contain other locations.");
            }

            // The one check create does not need: a new record has nothing beneath it yet, but an
            // existing zone can be pointed at something inside itself.
            if (await LocationAncestry.WouldCycleAsync(tenantUow, location.Id, parentId, ct))
            {
                return Result.Fail(
                    $"'{parent.Name}' sits inside '{location.Name}', so it cannot also contain it.");
            }
        }

        if (req.BranchId is { } branchId && branchId != location.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        if (req.CustomerId is { } customerId && customerId != location.CustomerId
            && await tenantUow.Repository<Customer>().GetByIdAsync(customerId, ct) is null)
        {
            return Result.Fail($"Could not find customer with ID '{customerId}'");
        }

        location.Name = req.Name;
        location.Type = req.Type;
        location.Address = req.Type is LocationType.Zone ? null : req.Address;
        location.ParentLocationId = req.ParentLocationId;
        location.Latitude = req.Latitude;
        location.Longitude = req.Longitude;
        location.Notes = req.Notes;
        location.BranchId = req.BranchId;
        location.CustomerId = req.CustomerId;
        location.DefaultCutoffTime = req.DefaultCutoffTime;
        location.ZoneRadiusMeters = req.ZoneRadiusMeters;
        location.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
