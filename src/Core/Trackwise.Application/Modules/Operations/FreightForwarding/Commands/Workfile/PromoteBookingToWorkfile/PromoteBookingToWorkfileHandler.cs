using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class PromoteBookingToWorkfileHandler(
    ITenantUnitOfWork tenantUow,
    IWorkfileReferenceGenerator referenceGenerator)
    : IAppRequestHandler<PromoteBookingToWorkfileCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PromoteBookingToWorkfileCommand req, CancellationToken ct)
    {
        var header = ToHeaderData(req);

        var series = WorkfileSeries.For(req.Type, req.Direction, req.Variant);
        var rules = WorkfileRules.For(req.Type);

        var customerId = req.CustomerId;

        if (rules.RequiresBookingNumber && !req.BookingId.HasValue)
        {
            return Result<Guid>.Fail($"A {req.Type} workfile must be promoted from a booking.");
        }

        if (rules.RequiresBillIndicator && !req.BillIndicator.HasValue)
        {
            return Result<Guid>.Fail($"A {req.Type} workfile requires an MBL/HBL indicator.");
        }

        if (req.BookingId.HasValue)
        {
            var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.BookingId.Value, ct);
            if (booking is null)
            {
                return Result<Guid>.Fail($"Could not find booking with ID '{req.BookingId}'");
            }

            if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.InProgress))
            {
                return Result<Guid>.Fail(
                    "Only a Confirmed or In Execution booking can be promoted to a workfile.");
            }

            // Mirrors the unique index; catching it here yields a readable message instead of a
            // DbUpdateException at save time.
            var alreadyPromoted = await tenantUow.Repository<Workfile>().Query()
                .AnyAsync(w => w.BookingId == booking.Id, ct);
            if (alreadyPromoted)
            {
                return Result<Guid>.Fail("This booking already has a workfile.");
            }

            customerId = booking.CustomerId;
        }

        if (!customerId.HasValue || customerId == Guid.Empty)
        {
            return Result<Guid>.Fail("A customer is required when the workfile has no booking.");
        }

        if (rules.RequiresDuplicateBlCheck && !string.IsNullOrWhiteSpace(req.BillOfLadingNumber))
        {
            var duplicate = await tenantUow.Repository<Workfile>().Query()
                .AnyAsync(w => w.BillOfLadingNumber == req.BillOfLadingNumber, ct);
            if (duplicate)
            {
                return Result<Guid>.Fail(
                    $"A workfile with BL number '{req.BillOfLadingNumber}' already exists.");
            }
        }

        var partnerCheck = await WorkfilePartnerValidation.ValidateAsync(
            tenantUow.Repository<BusinessPartner>(), header, ct);
        if (!partnerCheck.IsSuccess)
        {
            return Result<Guid>.Fail(partnerCheck.Error!);
        }

        var reference = req.ReferenceNumber?.Trim();
        if (string.IsNullOrEmpty(reference))
        {
            reference = await referenceGenerator.NextAsync(series, ct);
        }
        else
        {
            var taken = await tenantUow.Repository<Workfile>().Query()
                .AnyAsync(w => w.ReferenceNumber == reference, ct);
            if (taken)
            {
                return Result<Guid>.Fail($"Reference '{reference}' is already in use.");
            }
        }

        var workfile = WorkfileFactory.Create(
            req.Type, req.Direction, req.Variant, reference, customerId.Value, req.BookingId, header);

        await tenantUow.Repository<Workfile>().AddAsync(workfile, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<Guid>.Ok(workfile.Id);
    }

    private static WorkfileHeaderData ToHeaderData(PromoteBookingToWorkfileCommand req) => new()
    {
        BillOfLadingNumber = req.BillOfLadingNumber,
        BillIndicator = req.BillIndicator,
        VesselName = req.VesselName,
        VoyageNumber = req.VoyageNumber,
        Etd = req.Etd,
        Eta = req.Eta,
        Origin = req.Origin,
        Destination = req.Destination,
        Commodity = req.Commodity,
        ShippingLineId = req.ShippingLineId,
        CoLoaderId = req.CoLoaderId,
        ForwarderId = req.ForwarderId,
        TruckerId = req.TruckerId,
        BrokerId = req.BrokerId,
        CreditTermDays = req.CreditTermDays,
        CreditType = req.CreditType,
        ExchangeRate = req.ExchangeRate,
        TaggingCategory = req.TaggingCategory,
        IsAdvanceBilling = req.IsAdvanceBilling,
        ReportCategory = req.ReportCategory!.Value
    };
}
