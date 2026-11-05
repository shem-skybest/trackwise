using MediatR;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.Operations.RateAgreements;
using Trackwise.Application.Modules.Operations.Trips.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CreateTripFromBookingHandler(ITenantUnitOfWork tenantUow, ISender sender)
    : IAppRequestHandler<CreateTripFromBookingCommand, Result<CreateTripFromBookingResultDto>>
{
    public Task<Result<CreateTripFromBookingResultDto>> Handle(CreateTripFromBookingCommand req, CancellationToken ct) =>
        // One unit: a trip whose booking can't be moved on, or the other way round, is rolled back.
        tenantUow.ExecuteInTransactionAsync(token => CreateAsync(req, token), ct);

    private async Task<Result<CreateTripFromBookingResultDto>> CreateAsync(
        CreateTripFromBookingCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.BookingId, ct);
        if (booking is null)
        {
            return Fail($"Booking with ID '{req.BookingId}' not found.");
        }

        if (!booking.IsFromRateAgreement || booking.ServiceDate is not { } serviceDate)
        {
            return Fail("Only a Cold Chain booking can be turned into a trip here.");
        }

        if (booking.Status is not BookingStatus.Confirmed)
        {
            return Fail("Confirm the booking before making its trip.");
        }

        if (booking.PickupSiteId is not { } pickupSiteId)
        {
            return Fail("Set the pickup warehouse on the booking first.");
        }

        var drops = booking.Drops.OrderBy(d => d.SortOrder).ToList();
        var siteIds = drops.Select(d => d.SiteId).Append(pickupSiteId).Distinct().ToList();
        var sites = await tenantUow.Repository<Location>().Query()
            .Where(l => siteIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);

        var unplaced = siteIds
            .Select(id => sites.GetValueOrDefault(id))
            .FirstOrDefault(site => site?.Address is null || site.Latitude is null || site.Longitude is null);
        if (unplaced is not null || sites.Count != siteIds.Count)
        {
            return Fail(unplaced is null
                ? "A location on this booking no longer exists."
                : $"'{unplaced.Name}' has no address or map pin yet. Pin it under Master Data > Locations first.");
        }

        var customerName = await tenantUow.Repository<Customer>().Query()
            .Where(c => c.Id == booking.CustomerId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        var settings = tenantUow.GetCurrentTenant().Settings;
        var callTime = booking.CallTime ?? TimeOnly.MinValue;
        var pickup = sites[pickupSiteId];

        var loads = drops.Select(drop =>
        {
            var site = sites[drop.SiteId];
            return new CreateTripLoadCommand
            {
                Name = booking.ReferenceNumber is { } reference ? $"{reference} - {site.Name}" : site.Name,
                Type = LoadType.RefrigeratedGoods,
                OriginAddress = pickup.Address!,
                OriginLocation = PinOf(pickup),
                DestinationAddress = site.Address!,
                DestinationLocation = PinOf(site),
                // Straight-line metres, as the booking estimate read it: a distance-band card
                // (PFI's) prices the trip from this.
                Distance = PinOf(pickup).DistanceTo(PinOf(site)),
                AssignedDispatcherId = req.DispatcherId,
                CustomerId = booking.CustomerId,
                RequestedPickupDate = settings.ToUtc(serviceDate, callTime),
                RequestedDeliveryDate = site.DefaultCutoffTime is { } cutoff
                    // A cut-off earlier in the day than the call time is the next morning's.
                    ? settings.ToUtc(cutoff < callTime ? serviceDate.AddDays(1) : serviceDate, cutoff)
                    : null,
                Notes = drop.Notes,
                Commodity = booking.Commodity,
                PlannedBoxes = drop.PlannedBoxes,
                PlannedBags = drop.PlannedBags,
                OriginSiteId = pickupSiteId,
                DestinationSiteId = drop.SiteId,
                BookingId = booking.Id
            };
        }).ToList();

        var created = await sender.Send(new CreateTripCommand
        {
            Name = $"{booking.ReferenceNumber} {customerName}".Trim(),
            TruckId = req.TruckId,
            DriverId = req.DriverId,
            HelperId = req.HelperId,
            SecondHelperId = req.SecondHelperId,
            NewLoads = loads
        }, ct);

        if (!created.IsSuccess)
        {
            return Fail(created.Error!);
        }

        //booking.StartExecution();
        await tenantUow.SaveChangesAsync(ct);

        return Result<CreateTripFromBookingResultDto>.Ok(await PriceAsync(created.Value, serviceDate, ct));
    }

    /// <summary>
    /// Prices the new trip the way "Price from agreement" on the trip page does. A trip that can't
    /// be priced yet (no truck, no rate for a drop) is still made; the dispatcher prices it later.
    /// </summary>
    private async Task<CreateTripFromBookingResultDto> PriceAsync(Guid tripId, DateOnly serviceDate, CancellationToken ct)
    {
        var result = new CreateTripFromBookingResultDto { TripId = tripId };
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(tripId, ct);
        if (trip is null)
        {
            return result;
        }

        var quoted = await TripRateCalculator.QuoteAsync(tenantUow, trip, serviceDate, ct);
        if (!quoted.IsSuccess)
        {
            result.PricingError = quoted.Error;
            return result;
        }

        var (quote, ratedOn, dto) = quoted.Value!;
        try
        {
            trip.ApplyRate(quote, ratedOn, DateTime.UtcNow);
            await tenantUow.SaveChangesAsync(ct);
            result.PricedTotal = dto.Total;
        }
        catch (InvalidOperationException ex)
        {
            result.PricingError = ex.Message;
        }

        return result;
    }

    private static GeoPoint PinOf(Location site) => new(site.Longitude!.Value, site.Latitude!.Value);

    private static Result<CreateTripFromBookingResultDto> Fail(string message) =>
        Result<CreateTripFromBookingResultDto>.Fail(message);
}
