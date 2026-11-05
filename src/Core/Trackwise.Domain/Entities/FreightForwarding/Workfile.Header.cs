using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The editable half of a workfile header, passed as one value so callers cannot set half a file.
/// Every field is the value to store - merge policy ("null means leave alone") belongs to the
/// command handler, not here.
/// </summary>
public sealed record WorkfileHeaderData
{
    public string? BillOfLadingNumber { get; init; }
    public BillIndicator? BillIndicator { get; init; }
    public string? VesselName { get; init; }
    public string? VoyageNumber { get; init; }
    public DateTime? Etd { get; init; }
    public DateTime? Eta { get; init; }
    public string? Origin { get; init; }
    public string? Destination { get; init; }
    public string? Commodity { get; init; }
    public Guid? ShippingLineId { get; init; }
    public Guid? CoLoaderId { get; init; }
    public Guid? ForwarderId { get; init; }
    public Guid? TruckerId { get; init; }
    public Guid? BrokerId { get; init; }
    public int? CreditTermDays { get; init; }
    public CreditType? CreditType { get; init; }
    public decimal? ExchangeRate { get; init; }
    public decimal? FinalExchangeRate { get; init; }
    public string? TaggingCategory { get; init; }
    public bool IsAdvanceBilling { get; init; }
    public ReportCategory ReportCategory { get; init; }
}

public abstract partial class Workfile
{
    /// <summary>
    /// Seeds a newly created file. Separate from <see cref="UpdateHeader"/> because creation has no
    /// editability gate to satisfy - the file does not exist yet.
    /// </summary>
    protected void Initialize(
        string referenceNumber,
        Guid customerId,
        Guid? bookingId,
        WorkfileHeaderData header)
    {
        ReferenceNumber = referenceNumber;
        CustomerId = customerId;
        BookingId = bookingId;
        Status = WorkfileStatus.Open;
        ApplyHeader(header);
    }

    public void UpdateHeader(WorkfileHeaderData header)
    {
        if (IsReferenceLocked)
        {
            throw new InvalidOperationException(
                $"Workfile '{ReferenceNumber}' is locked by Accounting and cannot be edited.");
        }

        if (!WorkfileStatusMachine.IsEditable(Status))
        {
            throw new InvalidOperationException(
                $"Workfile '{ReferenceNumber}' is {Status} and cannot be edited.");
        }

        ApplyHeader(header);
        DomainEvents.Add(new WorkfileHeaderUpdated(Id, ReferenceNumber));
    }

    private void ApplyHeader(WorkfileHeaderData header)
    {
        BillOfLadingNumber = header.BillOfLadingNumber;
        BillIndicator = RequiresBillIndicator ? header.BillIndicator : null;
        VesselName = header.VesselName;
        VoyageNumber = header.VoyageNumber;
        Etd = header.Etd;
        Eta = header.Eta;
        Origin = header.Origin;
        Destination = header.Destination;
        Commodity = header.Commodity;
        ShippingLineId = header.ShippingLineId;
        CoLoaderId = header.CoLoaderId;
        ForwarderId = header.ForwarderId;
        TruckerId = header.TruckerId;
        BrokerId = header.BrokerId;
        CreditTermDays = header.CreditTermDays;
        CreditType = header.CreditType;
        ExchangeRate = header.ExchangeRate;
        FinalExchangeRate = header.FinalExchangeRate;
        TaggingCategory = header.TaggingCategory;
        IsAdvanceBilling = header.IsAdvanceBilling;
        ReportCategory = header.ReportCategory;
    }
}

public record WorkfileHeaderUpdated(Guid Id, string ReferenceNumber) : Core.IDomainEvent;
