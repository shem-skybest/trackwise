using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AddQuotationLine;

internal sealed class AddQuotationLineHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<AddQuotationLineCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddQuotationLineCommand req, CancellationToken ct)
    {
        var quotationRepo = tenantUow.Repository<Quotation>();

        // 1. Verify Quotation and check status
        var quotation = await quotationRepo.GetByIdAsync(req.QuotationId, ct);
        if (quotation is null)
            return Result<Guid>.Fail($"Quotation with ID '{req.QuotationId}' not found.");

        if (quotation.Status != QuotationStatus.Draft)
            return Result<Guid>.Fail("Lines can only be added to a Draft quotation.");

        // 2. Load the TariffRate WITH its ChargeCode and Tariff. The domain overload below needs the loaded
        //    ChargeCode to validate category/currency, and the line keeps the navigation for pricing.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rate = await tenantUow.Repository<TariffRate>().Query()
            .Include(r => r.ChargeCode)
            .Include(r => r.Tariff)
            .FirstOrDefaultAsync(r => r.Id == req.TariffRateId, ct);

        if (rate is null || rate.ChargeCode is null || rate.Tariff is null)
            return Result<Guid>.Fail($"Tariff rate with ID '{req.TariffRateId}' not found.");

        // 3. The rate must come from a tariff that is usable for this quotation.
        //    (Relax the mode check if you intentionally mix modes, e.g. Triangular quotes using Import rates.)
        var tariff = rate.Tariff;
        if (!tariff.IsActive || tariff.EffectiveFrom > today || (tariff.EffectiveTo.HasValue && tariff.EffectiveTo < today))
            return Result<Guid>.Fail($"Tariff '{tariff.Name}' is not active or not currently effective.");

        if (tariff.Mode != quotation.FreightMode)
            return Result<Guid>.Fail(
                $"Tariff '{tariff.Name}' is a {tariff.Mode} tariff but the quotation is {quotation.FreightMode}.");

        // 4. Percentage rates need a valuation base that can actually be resolved at pricing time.
        //    Fail here, not when the quote is submitted or issued.
        if (rate.PricingModel == PricingModel.Percentage && req.BaseAmount is null)
        {
            var baseKey = rate.PercentageBase?.Trim().ToUpperInvariant();

            if (baseKey == "CIF")
            {
                var cargoValue = quotation.CargoSpec?.DeclaredCargoValue;
                if (cargoValue is null || cargoValue.Amount <= 0m)
                    return Result<Guid>.Fail("This rate is a percentage of CIF value, but the quotation has no declared cargo value.");

                if (!string.Equals(cargoValue.Currency, quotation.Currency, StringComparison.OrdinalIgnoreCase))
                    return Result<Guid>.Fail(
                        $"Cargo is declared in {cargoValue.Currency} but the quotation is in {quotation.Currency}. Provide a BaseAmount in {quotation.Currency}.");
            }
            else if (baseKey != "BAS")
            {
                return Result<Guid>.Fail(
                    $"Percentage base '{rate.PercentageBase ?? "(none)"}' cannot be derived automatically. Provide a BaseAmount.");
            }
        }

        // 5. Add the line through the TariffRate overload so the aggregate validates the category against the
        //    charge code and checks FX rules. Domain exceptions become failed Results.
        QuotationLine line;
        try
        {
            line = quotation.AddLine(
                tariffRate: rate,
                category: rate.ChargeCode.Category,
                serviceType: req.ServiceType,
                quantity: req.Quantity,
                markupType: req.MarkupType,
                markupValue: req.MarkupValue,
                fxRate: req.FxRate,
                notes: req.Notes,
                baseAmount: req.BaseAmount);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Guid>.Fail(ex.Message);
        }

        // quotation is an EXISTING tracked entity (GetByIdAsync). The new line only reaches the
        // tracker via quotation.Lines, which EF treats as an update to a row that doesn't exist yet
        // unless it's explicitly added - see .claude/rules/backend/ef-persistence.md.
        await tenantUow.Repository<QuotationLine>().AddAsync(line, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(line.Id);
    }
}
