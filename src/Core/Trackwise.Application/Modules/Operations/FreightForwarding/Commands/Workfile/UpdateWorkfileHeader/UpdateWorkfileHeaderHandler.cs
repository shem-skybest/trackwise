using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class UpdateWorkfileHeaderHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateWorkfileHeaderCommand, Result>
{
    public async Task<Result> Handle(UpdateWorkfileHeaderCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        if (workfile.IsReferenceLocked)
        {
            return Result.Fail(
                $"Workfile '{workfile.ReferenceNumber}' is locked by Accounting and cannot be edited.");
        }

        if (!workfile.IsEditable)
        {
            return Result.Fail(
                $"Workfile '{workfile.ReferenceNumber}' is {workfile.Status} and cannot be edited.");
        }

        var header = Merge(req, workfile);

        if (workfile.RequiresBillIndicator && !header.BillIndicator.HasValue)
        {
            return Result.Fail($"A {workfile.Type} workfile requires an MBL/HBL indicator.");
        }

        if (workfile.RequiresDuplicateBlCheck
            && !string.IsNullOrWhiteSpace(header.BillOfLadingNumber)
            && header.BillOfLadingNumber != workfile.BillOfLadingNumber)
        {
            var duplicate = await tenantUow.Repository<Workfile>().Query()
                .AnyAsync(w => w.BillOfLadingNumber == header.BillOfLadingNumber && w.Id != workfile.Id, ct);
            if (duplicate)
            {
                return Result.Fail(
                    $"A workfile with BL number '{header.BillOfLadingNumber}' already exists.");
            }
        }

        var partnerCheck = await WorkfilePartnerValidation.ValidateAsync(
            tenantUow.Repository<BusinessPartner>(), header, ct);
        if (!partnerCheck.IsSuccess)
        {
            return partnerCheck;
        }

        workfile.UpdateHeader(header);

        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static WorkfileHeaderData Merge(UpdateWorkfileHeaderCommand req, Workfile current) => new()
    {
        BillOfLadingNumber = PropertyUpdater.UpdateIfChanged(req.BillOfLadingNumber, current.BillOfLadingNumber),
        BillIndicator = req.BillIndicator ?? current.BillIndicator,
        VesselName = PropertyUpdater.UpdateIfChanged(req.VesselName, current.VesselName),
        VoyageNumber = PropertyUpdater.UpdateIfChanged(req.VoyageNumber, current.VoyageNumber),
        Etd = req.Etd ?? current.Etd,
        Eta = req.Eta ?? current.Eta,
        Origin = PropertyUpdater.UpdateIfChanged(req.Origin, current.Origin),
        Destination = PropertyUpdater.UpdateIfChanged(req.Destination, current.Destination),
        Commodity = PropertyUpdater.UpdateIfChanged(req.Commodity, current.Commodity),
        ShippingLineId = req.ShippingLineId ?? current.ShippingLineId,
        CoLoaderId = req.CoLoaderId ?? current.CoLoaderId,
        ForwarderId = req.ForwarderId ?? current.ForwarderId,
        TruckerId = req.TruckerId ?? current.TruckerId,
        BrokerId = req.BrokerId ?? current.BrokerId,
        CreditTermDays = req.CreditTermDays ?? current.CreditTermDays,
        CreditType = req.CreditType ?? current.CreditType,
        ExchangeRate = req.ExchangeRate ?? current.ExchangeRate,
        FinalExchangeRate = req.FinalExchangeRate ?? current.FinalExchangeRate,
        TaggingCategory = PropertyUpdater.UpdateIfChanged(req.TaggingCategory, current.TaggingCategory),
        IsAdvanceBilling = req.IsAdvanceBilling ?? current.IsAdvanceBilling,
        ReportCategory = req.ReportCategory ?? current.ReportCategory
    };
}
