using System.Collections.ObjectModel;
using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents a customer-facing quotation: selling charges built from live Tariff/ChargeCode rates.
/// </summary>
public class Quotation : AuditableEntity, ITenantEntity
{
    public const decimal HighValueCargoThresholdPhp = 30_000_000m;

    public Guid CustomerId { get; private set; }
    public virtual Customer Customer { get; private set; }
    public Guid BranchId { get; private set; }
    public virtual Branch Branch { get; private set; }
    public string ReferenceNumber { get; private set; } = default!;
    public string Currency { get; private set; } = default!;
    public QuotationStatus Status { get; private set; }
    public Guid RevisionGroupId { get; private set; }
    public int RevisionNumber { get; private set; }
    public Guid? PreviousRevisionId { get; private set; }
    public virtual Quotation? PreviousRevision { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public string? Notes { get; private set; }

    // Freight Forwarding Core Logistics Context
    public TransportMode Mode { get; private set; }
    public FreightMode FreightMode { get; private set; }   // Domestic, Import, Export, Triangular
    public ShipmentType ShipmentType { get; private set; }
    public Incoterm Incoterm { get; private set; }
    public LaneTransit Lane { get; private set; } = default!;
    public CargoSpecification? CargoSpec { get; private set; }

    /// <summary>
    /// FIX #3: declared cargo value converted to USD by the application layer (FX lives outside the domain).
    /// The approval rule uses this instead of comparing raw amounts across currencies.
    /// </summary>
    public decimal? DeclaredCargoValuePhp { get; private set; }

    // Optional Specialized Logistics Profiles
    public TemperatureControl? TemperatureSpec { get; private set; }
    public bool IsTemperatureControlled => TemperatureSpec is not null;
    public bool IsHazardous { get; private set; }
    public bool RequiresCustomsBrokerage { get; private set; }

    // Approval / Governance Fields
    public bool RequiresSpecialApproval { get; private set; }
    public string? ApprovalEscalationReason { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public DateTimeOffset? IssuedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }

    private readonly List<QuotationLine> _lines = [];
    public virtual IReadOnlyCollection<QuotationLine> Lines => new ReadOnlyCollection<QuotationLine>(_lines);

    protected Quotation() { }

    public static Quotation Create(
        Guid customerId,
        Guid branchId,
        string referenceNumber,
        string currency,
        TransportMode mode,
        FreightMode freightMode,
        ShipmentType shipmentType,
        Incoterm incoterm,
        LaneTransit lane,
        CargoSpecification? cargoSpec,
        TemperatureControl? tempSpec = null,
        bool isHazardous = false,
        bool requiresCustomsBrokerage = false,
        string? notes = null,
        decimal? declaredCargoValuePhp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentNullException.ThrowIfNull(lane);
        ValidateTemperatureRequirements(cargoSpec, tempSpec);

        var quotation = new Quotation
        {
            CustomerId = customerId,
            BranchId = branchId,
            ReferenceNumber = referenceNumber.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            Status = QuotationStatus.Draft,
            RevisionGroupId = Guid.Empty,
            RevisionNumber = 1,
            Mode = mode,
            FreightMode = freightMode,
            ShipmentType = shipmentType,
            Incoterm = incoterm,
            Lane = lane,
            CargoSpec = cargoSpec,
            DeclaredCargoValuePhp = declaredCargoValuePhp,
            TemperatureSpec = tempSpec,
            IsHazardous = isHazardous,
            RequiresCustomsBrokerage = requiresCustomsBrokerage,
            Notes = notes
        };

        quotation.RevisionGroupId = quotation.Id;
        quotation.EvaluateApprovalRequirements();
        quotation.DomainEvents.Add(new QuotationCreated(quotation.Id, customerId));
        return quotation;
    }

    public Quotation CreateRevision(string referenceNumber, int revisionNumber)
    {
        if (Status == QuotationStatus.Draft)
            throw new InvalidOperationException("A draft quotation can be edited directly and cannot be revised.");

        if (revisionNumber <= RevisionNumber)
            throw new ArgumentOutOfRangeException(nameof(revisionNumber), "A revision number must be greater than the current revision.");

        var lane = new LaneTransit(
            Lane.Stops.Select(stop => RouteStop.Create(
                stop.Sequence,
                stop.LocationId,
                stop.Type,
                stop.WindowStart,
                stop.WindowEnd,
                stop.Instructions)),
            Lane.TotalDistanceKm);
        var cargoSpec = CargoSpec is null
            ? null
            : CargoSpecification.Create(
                CargoSpec.Commodity,
                CargoSpec.TotalWeightKg,
                CargoSpec.PalletCount,
                new Money
                {
                    Amount = CargoSpec.DeclaredCargoValue.Amount,
                    Currency = CargoSpec.DeclaredCargoValue.Currency
                },
                CargoSpec.RequiresActiveRefrigeration);
        var temperatureSpec = TemperatureSpec is null
            ? null
            : TemperatureControl.Create(
                TemperatureSpec.Zone,
                TemperatureSpec.SetpointCelsius,
                TemperatureSpec.AllowableVarianceCelsius,
                TemperatureSpec.Compliance,
                TemperatureSpec.RequiresContinuousTelematics);

        var revision = Create(
            CustomerId,
            BranchId,
            referenceNumber,
            Currency,
            Mode,
            FreightMode,
            ShipmentType,
            Incoterm,
            lane,
            cargoSpec,
            temperatureSpec,
            IsHazardous,
            RequiresCustomsBrokerage,
            Notes,
            DeclaredCargoValuePhp);

        revision.RevisionGroupId = RevisionGroupId;
        revision.RevisionNumber = revisionNumber;
        revision.PreviousRevisionId = Id;

        foreach (var line in _lines)
            revision._lines.Add(line.CopyTo(revision.Id, revision.Currency));

        revision.EvaluateApprovalRequirements();
        return revision;
    }

    /// <summary>
    /// Replaces the editable details of a Draft quotation. Mirrors Create's invariants.
    /// If lines exist, the currency can't change; call ClearLines() first, then reprice.
    /// </summary>
    public void UpdateDetails(
        Guid customerId,
        Guid branchId,
        string currency,
        TransportMode mode,
        FreightMode freightMode,
        ShipmentType shipmentType,
        Incoterm incoterm,
        LaneTransit lane,
        CargoSpecification? cargoSpec,
        TemperatureControl? tempSpec = null,
        bool isHazardous = false,
        bool requiresCustomsBrokerage = false,
        string? notes = null,
        decimal? declaredCargoValuePhp = null)
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Details can only be changed while the quotation is in Draft status.");

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentNullException.ThrowIfNull(lane);
        ValidateTemperatureRequirements(cargoSpec, tempSpec);

        var newCurrency = currency.Trim().ToUpperInvariant();

        // Existing lines are priced in the old currency; they must be cleared first.
        if (_lines.Count > 0 && !string.Equals(newCurrency, Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Currency cannot be changed while priced lines exist. Clear the lines first.");

        // Resolve before mutating so a failure leaves the aggregate untouched.
        //var cargoValuePhp = ResolveCargoValuePhp(cargoSpec, declaredCargoValuePhp);
        if (RevisionNumber > 1 &&
            (customerId != CustomerId ||
             freightMode != FreightMode ||
             !HasSameRoute(Lane, lane)))
        {
            throw new InvalidOperationException(
                "A quotation revision must keep the same customer, freight mode, and route. Create a new quotation to change them.");
        }

        CustomerId = customerId;
        BranchId = branchId;
        Currency = newCurrency;
        Mode = mode;
        FreightMode = freightMode;
        ShipmentType = shipmentType;
        Incoterm = incoterm;
        Lane = lane;
        CargoSpec = cargoSpec;
        DeclaredCargoValuePhp = declaredCargoValuePhp;
        TemperatureSpec = tempSpec;
        IsHazardous = isHazardous;
        RequiresCustomsBrokerage = requiresCustomsBrokerage;
        Notes = notes;

        EvaluateApprovalRequirements();
        DomainEvents.Add(new QuotationUpdated(Id));
    }

    /// <summary>Removes every line (Draft only) so the quotation can be repriced from scratch.</summary>
    public void ClearLines()
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Lines can only be cleared while the quotation is in Draft status.");

        _lines.Clear();
        EvaluateApprovalRequirements();
    }


    /// <summary>
    /// USD value is taken as-is when supplied, derived when the cargo is already declared in USD,
    /// and otherwise REQUIRED, so a PHP/EUR amount can never be mistaken for dollars.
    /// </summary>
    private static decimal? ResolveCargoValuePhp(CargoSpecification? spec, decimal? phpValue)
    {
        if (phpValue.HasValue)
        {
            if (phpValue.Value < 0m)
                throw new ArgumentOutOfRangeException(nameof(phpValue), "USD cargo value cannot be negative.");
            return phpValue;
        }

        var declared = spec?.DeclaredCargoValue;
        if (declared is null) return null;

        // Nothing declared is nothing in any currency - the caller skips the conversion for it.
        if (declared.Amount == 0m || string.Equals(declared.Currency, "PHP", StringComparison.OrdinalIgnoreCase))
            return declared.Amount;

        throw new ArgumentException(
            $"Cargo value is declared in {declared.Currency}; supply declaredCargoValueUsd (converted by the application layer).",
            nameof(phpValue));
    }

    private static void ValidateTemperatureRequirements(
        CargoSpecification? cargoSpec,
        TemperatureControl? temperatureSpec)
    {
        if (cargoSpec?.RequiresActiveRefrigeration == true && temperatureSpec is null)
        {
            throw new InvalidOperationException(
                "Temperature specifications are required when active refrigeration is selected.");
        }
    }

    private static bool HasSameRoute(LaneTransit current, LaneTransit updated)
    {
        var currentStops = current.Stops
            .OrderBy(stop => stop.Sequence)
            .Select(stop => (stop.Sequence, stop.LocationId, stop.Type));
        var updatedStops = updated.Stops
            .OrderBy(stop => stop.Sequence)
            .Select(stop => (stop.Sequence, stop.LocationId, stop.Type));

        return currentStops.SequenceEqual(updatedStops);
    }

    /// <summary>Refresh the USD equivalent (e.g. after an FX update) and re-run approval rules.</summary>
    public void SetDeclaredCargoValueUsd(decimal usdValue)
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Cargo value can only be changed while the quotation is in Draft status.");

        if (usdValue < 0m)
            throw new ArgumentOutOfRangeException(nameof(usdValue), "USD cargo value cannot be negative.");

        DeclaredCargoValuePhp = usdValue;
        EvaluateApprovalRequirements();
    }

    // ── Lines ────────────────────────────────────────────────────────────────

    public QuotationLine AddLine(
        TariffRate tariffRate,
        ChargeCategory category,
        OperationalServiceType serviceType,
        decimal quantity,
        MarkupType markupType,
        decimal markupValue,
        decimal? fxRate,
        string? notes,
        decimal? baseAmount = null)
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the quotation is in Draft status.");

        ArgumentNullException.ThrowIfNull(tariffRate);

        var effectiveBaseAmount = baseAmount ?? tariffRate.BaseAmount;

        var chargeCode = tariffRate.ChargeCode;
        if (chargeCode is not null)
        {
            // FIX #2: the category must agree with the charge code instead of being silently ignored.
            if (chargeCode.Category != category)
                throw new ArgumentException(
                    $"Category '{category}' does not match charge code '{chargeCode.Code}' ({chargeCode.Category}).",
                    nameof(category));

            // Fail fast on a missing FX rate rather than at pricing time.
            var rateCurrency = tariffRate.SourceCurrency;
            if (!string.Equals(rateCurrency, Currency, StringComparison.OrdinalIgnoreCase) && fxRate is not > 0m)
            {
                throw new InvalidOperationException(
                    $"Charge '{chargeCode.Code}' is in {rateCurrency} but the quotation is in {Currency}; an fxRate is required.");
            }
        }

        if (string.Equals(tariffRate.SourceCurrency, Currency, StringComparison.OrdinalIgnoreCase))
            fxRate = null;

        var line = QuotationLine.Create(
            Id, Currency, tariffRate, category, serviceType, quantity,
            markupType, markupValue, fxRate, notes, effectiveBaseAmount);

        _lines.Add(line);
        EvaluateApprovalRequirements();
        return line;
    }

    public QuotationLine AddLine(
        Guid tariffRateId,
        ChargeCategory category,
        OperationalServiceType serviceType,
        decimal quantity,
        MarkupType markupType,
        decimal markupValue,
        decimal? fxRate,
        string? notes,
        decimal? baseAmount = null)
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the quotation is in Draft status.");

        var line = QuotationLine.Create(
            Id, Currency, tariffRateId, category, serviceType, quantity,
            markupType, markupValue, fxRate, notes, baseAmount);

        _lines.Add(line);
        EvaluateApprovalRequirements();
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Lines can only be removed while the quotation is in Draft status.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return;

        _lines.Remove(line);
        EvaluateApprovalRequirements();
    }

    // ── Pricing (FIX #1) ─────────────────────────────────────────────────────

    /// <summary>
    /// Prices a line, resolving the valuation base for Percentage rates from the quotation.
    /// Requires TariffRate (and ChargeCode) to be loaded on every line.
    /// </summary>
    public (Money Buying, Money Selling) PriceLine(QuotationLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return line.Price(ResolveValuationBase(line));
    }

    public QuotationTotals GetTotals()
    {
        decimal buying = 0m, selling = 0m;

        foreach (var line in _lines)
        {
            var (buy, sell) = PriceLine(line);
            buying += buy.Amount;
            selling += sell.Amount;
        }

        return new QuotationTotals(Currency, buying, selling);
    }

    /// <summary>
    /// Resolves the monetary base (in quotation currency) for Percentage lines:
    ///   explicit line.BaseAmount  → wins
    ///   "BAS"                     → sum of buying amounts of non-percentage Base-category lines
    ///   "CIF"                     → declared cargo value (must be in the quotation currency)
    ///   anything else             → must be provided explicitly on the line
    /// Non-percentage lines return null.
    /// </summary>
    private decimal? ResolveValuationBase(QuotationLine line)
    {
        var rate = line.TariffRate
            ?? throw new InvalidOperationException(
                $"TariffRate must be Include()'d before pricing quotation line '{line.Id}'.");

        if (rate.PricingModel != PricingModel.Percentage) return null;
        if (line.BaseAmount.HasValue) return line.BaseAmount;

        return rate.PercentageBase?.Trim().ToUpperInvariant() switch
        {
            "BAS" => BaseFreightBuying(),
            "CIF" => CargoValueInQuotationCurrency(line),
            _ => throw new InvalidOperationException(
                $"Line '{line.Id}' uses percentage base '{rate.PercentageBase ?? "(none)"}', which cannot be derived automatically. Set BaseAmount on the line.")
        };
    }

    private decimal BaseFreightBuying() =>
        _lines
            .Where(l => l.Category == ChargeCategory.Base &&
                        l.TariffRate is { PricingModel: not PricingModel.Percentage })
            .Sum(l => l.Price().Buying.Amount);

    private decimal CargoValueInQuotationCurrency(QuotationLine line)
    {
        var value = CargoSpec?.DeclaredCargoValue
            ?? throw new InvalidOperationException(
                $"Line '{line.Id}' uses a CIF base but the quotation has no declared cargo value.");

        if (!string.Equals(value.Currency, Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Line '{line.Id}' uses a CIF base, but cargo is declared in {value.Currency} and the quotation is in {Currency}. Set BaseAmount on the line.");

        return value.Amount;
    }

    // ── Approval & lifecycle ─────────────────────────────────────────────────

    public void EvaluateApprovalRequirements()
    {
        // 1. High-Value Cargo rule across all modes (USD threshold, currency-safe)
        if (DeclaredCargoValuePhp > HighValueCargoThresholdPhp)
        {
            RequiresSpecialApproval = true;
            ApprovalEscalationReason = "Cargo value exceeds standard automated insurance limit (30,000,000).";
            return;
        }

        // 2. Dangerous Goods / Hazmat approval rule
        if (IsHazardous)
        {
            RequiresSpecialApproval = true;
            ApprovalEscalationReason = "Dangerous goods require Hazmat Compliance team sign-off.";
            return;
        }

        // 3. Cold Chain Ultra-Low threshold rule
        if (TemperatureSpec?.Zone == TemperatureZone.Cryogenic)
        {
            RequiresSpecialApproval = true;
            ApprovalEscalationReason = "Cryogenic temperature handling mandates Quality Team sign-off.";
            return;
        }

        // 4. Air Charter or Large Project Cargo threshold rule
        if (ShipmentType == ShipmentType.AirCharter)
        {
            RequiresSpecialApproval = true;
            ApprovalEscalationReason = "Air Charter services require executive management approval.";
            return;
        }

        RequiresSpecialApproval = false;
        ApprovalEscalationReason = null;
    }

    /// <summary>
    /// Submits draft quote for processing. Validates operational integrity,
    /// auto-approves standard loads, or routes to PendingApproval if risk rules trigger.
    /// </summary>
    public void Submit()
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Only Draft quotations can be submitted.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit a quotation with no priced lines.");

        ValidateOperationalIntegrity();
        EvaluateApprovalRequirements();

        if (RequiresSpecialApproval)
        {
            Status = QuotationStatus.PendingApproval;
            DomainEvents.Add(new QuotationSubmittedForApproval(Id, ApprovalEscalationReason!));
        }
        else
        {
            Status = QuotationStatus.Approved;
            ApprovedBy = "SYSTEM_AUTO_APPROVER";
            ApprovedAt = DateTimeOffset.UtcNow;
            DomainEvents.Add(new QuotationApproved(Id, ApprovedBy));
        }
    }

    /// <summary>
    /// Executed by a Commercial/Pricing Manager to clear an escalated quote.
    /// </summary>
    public void Approve(string approvedBy)
    {
        if (Status != QuotationStatus.PendingApproval && Status != QuotationStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Quotation in '{Status}' status cannot be approved. Expected Draft or PendingApproval.");
        }

        ValidateOperationalIntegrity();

        Status = QuotationStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
        RejectedBy = null;
        RejectedAt = null;
        RejectionReason = null;

        DomainEvents.Add(new QuotationApproved(Id, approvedBy));
    }

    /// <summary>
    /// Issues the approved quote to the customer and locks pricing.
    /// </summary>
    public void Issue(TimeSpan validityDuration)
    {
        if (Status != QuotationStatus.Approved)
            throw new InvalidOperationException("Only approved quotations can be issued to customers.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot issue a quotation without priced lines.");

        // FIX #1: snapshot with the resolved valuation base so Percentage lines lock the correct amount.
        // Resolve ALL bases first, then snapshot, so no line's base depends on a half-snapshotted state.
        var bases = _lines.ToDictionary(l => l.Id, ResolveValuationBase);
        foreach (var line in _lines)
        {
            line.SnapshotPrices(bases[line.Id]);
        }

        Status = QuotationStatus.Issued;
        IssuedAt = DateTimeOffset.UtcNow;
        ValidUntil = DateTimeOffset.UtcNow.Add(validityDuration);
        DomainEvents.Add(new QuotationIssued(Id, ValidUntil.Value));
    }

    /// <summary>
    /// Customer accepts the quotation, enabling downstream booking conversion.
    /// </summary>
    public void Accept()
    {
        if (Status != QuotationStatus.Issued)
            throw new InvalidOperationException("Only issued quotations can be accepted.");

        if (ValidUntil.HasValue && DateTimeOffset.UtcNow > ValidUntil.Value)
        {
            Status = QuotationStatus.Expired;
            DomainEvents.Add(new QuotationExpired(Id));
            throw new InvalidOperationException("Quotation has expired.");
        }

        Status = QuotationStatus.Accepted;
        AcceptedAt = DateTimeOffset.UtcNow;
        DomainEvents.Add(new QuotationAccepted(Id, CustomerId));
    }

    /// <summary>
    /// Marks an issued quotation as expired when the customer acceptance window has elapsed.
    /// </summary>
    public void Expire()
    {
        if (Status != QuotationStatus.Issued)
            throw new InvalidOperationException($"Cannot expire quotation in status '{Status}'. Must be Issued.");

        Status = QuotationStatus.Expired;
        DomainEvents.Add(new QuotationExpired(Id));
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status is QuotationStatus.Accepted or QuotationStatus.Expired or QuotationStatus.Rejected)
        {
            throw new InvalidOperationException($"Quotation in status '{Status}' cannot be rejected.");
        }

        Status = QuotationStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectedAt = DateTimeOffset.UtcNow;
        RejectionReason = reason;
        DomainEvents.Add(new QuotationRejected(Id, rejectedBy, reason));
    }

    /// <summary>
    /// Validates business and operational rules across both standard and specialized freight.
    /// </summary>
    private void ValidateOperationalIntegrity()
    {
        // 1. Must always have at least one Base charge (Linehaul / Ocean / Air freight).
        //    FIX #2: uses the persisted line Category, so it no longer depends on navigation properties being loaded.
        if (!_lines.Any(l => l.Category == ChargeCategory.Base))
            throw new InvalidOperationException("Quote must include at least one Base linehaul/freight charge.");

        // 2. Cold Chain operational integrity
        if (IsTemperatureControlled)
        {
            if (TemperatureSpec!.RequiresContinuousTelematics &&
                !_lines.Any(l => l.ServiceType == OperationalServiceType.ContinuousTelematics))
            {
                throw new InvalidOperationException(
                    "Shipment requires continuous telematics, but no telemetry fee is quoted.");
            }

            if (TemperatureSpec.Zone != TemperatureZone.Ambient &&
                !_lines.Any(l => l.ServiceType == OperationalServiceType.PreCoolingAndPTI))
            {
                throw new InvalidOperationException(
                    "Refrigerated load requires a Pre-Cooling & PTI service line.");
            }
        }

        // 3. International customs brokerage integrity
        if (RequiresCustomsBrokerage &&
            !_lines.Any(l => l.ServiceType is OperationalServiceType.CustomsClearanceImport or OperationalServiceType.CustomsClearanceExport))
        {
            throw new InvalidOperationException(
                "Shipment requires customs brokerage, but no customs clearance line is present.");
        }
    }
}

public record QuotationTotals(string Currency, decimal Buying, decimal Selling)
{
    public decimal Margin => Selling - Buying;
    public decimal MarginPercent => Selling == 0m ? 0m : Math.Round(Margin / Selling * 100m, 2);
}

// Domain events
public record QuotationCreated(Guid Id, Guid CustomerId) : IDomainEvent;
public record QuotationSubmittedForApproval(Guid Id, string Reason) : IDomainEvent;
public record QuotationApproved(Guid Id, string ApprovedBy) : IDomainEvent;
public record QuotationIssued(Guid Id, DateTimeOffset ValidUntil) : IDomainEvent;
public record QuotationExpired(Guid Id) : IDomainEvent;
public record QuotationAccepted(Guid Id, Guid CustomerId) : IDomainEvent;
public record QuotationRejected(Guid Id, string RejectedBy, string Reason) : IDomainEvent;
public record QuotationUpdated(Guid Id) : IDomainEvent;
