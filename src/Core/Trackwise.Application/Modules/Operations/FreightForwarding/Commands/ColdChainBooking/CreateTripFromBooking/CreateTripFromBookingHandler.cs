using MediatR;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
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
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .SingleOrDefaultAsync(b => b.Id == req.BookingId, ct);
        if (booking is null)
        {
            return Fail($"Booking with ID '{req.BookingId}' not found.");
        }

        if (!booking.IsTemperatureControlled)
        {
            return Fail("Only a Cold Chain booking can be turned into a trip here.");
        }

        if (booking.Status is not BookingStatus.Confirmed)
        {
            return Fail("Confirm the booking before making its trip.");
        }

        // The route is now managed by the LaneTransit value object.
        var pickupStop = booking.Lane?.FirstPickup;
        if (pickupStop is null)
        {
            return Fail("The booking does not have a valid pickup stop configured on its lane.");
        }

        var pickupSiteId = pickupStop.LocationId;

        // Treat all subsequent stops in the sequence as drops/deliveries
        var drops = booking.Lane!.Stops
            .Where(s => s.Sequence > pickupStop.Sequence)
            .OrderBy(s => s.Sequence)
            .ToList();

        if (drops.Count == 0)
        {
            return Fail("The booking does not have any delivery stops configured on its lane.");
        }

        var siteIds = drops.Select(d => d.LocationId).Append(pickupSiteId).Distinct().ToList();
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

        // Derive ServiceDate and CallTime from the new RequestedPickupStart (FirstPickup.WindowStart)
        var serviceDate = booking.RequestedPickupStart.HasValue
            ? DateOnly.FromDateTime(booking.RequestedPickupStart.Value.UtcDateTime)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var callTime = booking.RequestedPickupStart.HasValue
            ? TimeOnly.FromDateTime(booking.RequestedPickupStart.Value.UtcDateTime)
            : TimeOnly.MinValue;

        var pickup = sites[pickupSiteId];

        var bookingTotal = booking.GetTotals().Selling;
        var amountPerDrop = drops.Count == 0 ? 0m : bookingTotal / drops.Count;
        var allocatedAmount = 0m;

        var loads = drops.Select((drop, index) =>
        {
            var site = sites[drop.LocationId];
            var deliveryCost = index == drops.Count - 1
                ? bookingTotal - allocatedAmount
                : amountPerDrop;
            allocatedAmount += deliveryCost;

            return new CreateTripLoadCommand
            {
                Name = booking.BookingNumber is { } reference ? $"{reference} - {site.Name}" : site.Name,
                Type = LoadType.RefrigeratedGoods,
                OriginAddress = pickup.Address!,
                OriginLocation = PinOf(pickup),
                DestinationAddress = site.Address!,
                DestinationLocation = PinOf(site),
                DeliveryCost = deliveryCost,
                Distance = PinOf(pickup).DistanceTo(PinOf(site)),
                AssignedDispatcherId = req.DispatcherId,
                CustomerId = booking.CustomerId,
                RequestedPickupDate = pickupStop.WindowStart?.UtcDateTime,
                RequestedDeliveryDate = drop.WindowEnd?.UtcDateTime ?? (site.DefaultCutoffTime is { } cutoff
                    ? settings.ToUtc(cutoff < callTime ? serviceDate.AddDays(1) : serviceDate, cutoff)
                    : null),
                Notes = booking.Notes,
                Commodity = null,
                PlannedBoxes = null,
                PlannedBags = null,
                OriginSiteId = pickupSiteId,
                DestinationSiteId = drop.LocationId,
                BookingId = booking.Id
            };
        }).ToList();

        var created = await sender.Send(new CreateTripCommand
        {
            Name = $"{booking.BookingNumber} {customerName}".Trim(),
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

        // Replaces the old booking.StartExecution() based on your new lifecycle methods
        booking.MarkInProgress();
        await tenantUow.SaveChangesAsync(ct);

        return Result<CreateTripFromBookingResultDto>.Ok(new CreateTripFromBookingResultDto
        {
            TripId = created.Value,
            PricedTotal = bookingTotal
        });
    }

    private static GeoPoint PinOf(Location site) => new(site.Longitude!.Value, site.Latitude!.Value);

    private static Result<CreateTripFromBookingResultDto> Fail(string message) =>
        Result<CreateTripFromBookingResultDto>.Fail(message);
}
