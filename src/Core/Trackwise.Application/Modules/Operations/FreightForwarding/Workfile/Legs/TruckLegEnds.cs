using Trackwise.Application.Abstractions.Geocoding;
using Trackwise.Application.Modules.Operations.Loads;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Legs;

/// <summary>Where a leg's loads start or end, ready to put on a <see cref="Load"/>.</summary>
internal sealed record ResolvedLegEnd(Address Address, GeoPoint Location, Guid? TerminalId, Guid? SiteId);

/// <summary>
/// Resolves a truck leg's two ends - a port or a saved site - into the address and coordinates a
/// load needs.
/// <para>
/// A port has no stored coordinates, so it is geocoded from its address; a site uses its own pin
/// when it has one. A leg that can't be placed on the map is refused rather than saved at 0,0,
/// because the proximity tracker and route distance would treat that as a real place.
/// </para>
/// </summary>
internal static class TruckLegEnds
{
    public static async Task<Result<ResolvedLegEnd>> ResolveAsync(
        ITenantUnitOfWork tenantUow,
        IGeocodingService geocoding,
        TruckLegEnd end,
        Guid customerId,
        CancellationToken ct)
    {
        if (end.TerminalId is { } terminalId)
        {
            var terminal = await tenantUow.Repository<Terminal>().GetByIdAsync(terminalId, ct);
            if (terminal is null || terminal.MergedIntoId is not null || !terminal.IsActive)
            {
                return Result<ResolvedLegEnd>.Fail("That port is not a current record.");
            }

            var point = await GeocodeAsync(geocoding, terminal.Address, ct);
            return point.IsSuccess
                ? Result<ResolvedLegEnd>.Ok(new ResolvedLegEnd(terminal.Address, point.Value!, terminal.Id, null))
                : Result<ResolvedLegEnd>.Fail($"Couldn't find port '{terminal.Name}' on the map - check its address.");
        }

        var site = await tenantUow.Repository<Location>().GetByIdAsync(end.SiteId!.Value, ct);
        if (site is null || site.MergedIntoId is not null)
        {
            return Result<ResolvedLegEnd>.Fail("That site is not a current record.");
        }

        if (LoadSiteRules.Refusal(site, customerId, isNewLink: true) is { } refusal)
        {
            return Result<ResolvedLegEnd>.Fail(refusal);
        }

        if (site.Address is null)
        {
            return Result<ResolvedLegEnd>.Fail($"Site '{site.Name}' has no address yet.");
        }

        if (site is { Latitude: { } lat, Longitude: { } lng })
        {
            return Result<ResolvedLegEnd>.Ok(new ResolvedLegEnd(site.Address, new GeoPoint(lng, lat), null, site.Id));
        }

        var sitePoint = await GeocodeAsync(geocoding, site.Address, ct);
        return sitePoint.IsSuccess
            ? Result<ResolvedLegEnd>.Ok(new ResolvedLegEnd(site.Address, sitePoint.Value!, null, site.Id))
            : Result<ResolvedLegEnd>.Fail($"Couldn't find site '{site.Name}' on the map - check its address.");
    }

    private static Task<Result<GeoPoint>> GeocodeAsync(IGeocodingService geocoding, Address address, CancellationToken ct) =>
        geocoding.GeocodeAddressAsync(
            address.StreetAddress, address.CityOrMunicipality, address.Province, address.Zipcode, address.Country, ct);
}
