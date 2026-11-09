using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.Shared.Models;

/// <summary>One planned drop as a Cold Chain booking form sends it.</summary>
public class BookingDropInput
{
    public Guid SiteId { get; set; }
    public int? PlannedBoxes { get; set; }
    public int? PlannedBags { get; set; }
    public string? Notes { get; set; }
}

/// <summary>A planned drop with its delivery location resolved for display.</summary>
public class BookingDropDto
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = default!;
    public string? SiteCode { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public TimeOnly? CutoffTime { get; set; }
    public int? PlannedBoxes { get; set; }
    public int? PlannedBags { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// What a Cold Chain booking would cost under the client's rate agreement. <see cref="Error"/> is set
/// instead of the amounts when it can't be priced (no agreement in force, no rate for a drop).
/// </summary>
public class BookingEstimateDto
{
    public Guid? AgreementId { get; set; }
    public string? AgreementName { get; set; }
    public string? LineLabel { get; set; }
    public decimal? BaseAmount { get; set; }
    public decimal? FuelAmount { get; set; }
    public string? FuelBasis { get; set; }
    public int ExtraDrops { get; set; }
    public decimal? ExtraDropAmount { get; set; }
    public decimal? Total { get; set; }
    public decimal? DieselPrice { get; set; }
    public string Currency { get; set; } = "PHP";
    public List<string> Warnings { get; set; } = [];
    public string? Error { get; set; }
}

/// <summary>A trip made from a booking, for linking to it from the booking page.</summary>
public class BookingTripDto
{
    public Guid TripId { get; set; }
    public long Number { get; set; }
    public string Name { get; set; } = default!;
    public TripStatus Status { get; set; }
}

/// <summary>Request body for turning a confirmed Cold Chain booking into a trip.</summary>
public class CreateTripFromBookingRequest
{
    public Guid? TruckId { get; set; }

    /// <summary>Leave the crew empty to take the crew planned for the truck that day.</summary>
    public Guid? DriverId { get; set; }

    public Guid? HelperId { get; set; }
    public Guid? SecondHelperId { get; set; }
}

/// <summary>Request body for repeating a Cold Chain booking on other days (a standing order).</summary>
public class RepeatColdChainBookingRequest
{
    public List<DateOnly> Dates { get; set; } = [];

    /// <summary>Confirm the copies straight away instead of leaving them pending.</summary>
    public bool Confirm { get; set; }
}

/// <summary>One booking a standing order made.</summary>
public class RepeatedBookingDto
{
    public Guid Id { get; set; }

    /// <summary>Null while numbers are typed in from nSFS; each copy takes its own later.</summary>
    public string? ReferenceNumber { get; set; }

    public DateOnly ServiceDate { get; set; }
    public BookingStatus Status { get; set; }
}

/// <summary>A Cold Chain booking with no trip yet, as the dispatcher's planning board lists it.</summary>
public class UnassignedBookingDto
{
    public Guid Id { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;

    /// <summary>Pending (still to confirm) or Confirmed (waiting for a truck).</summary>
    public BookingStatus Status { get; set; }

    public DateOnly? ServiceDate { get; set; }
    public TimeOnly? CallTime { get; set; }
    public TruckTonnageClass? TruckSize { get; set; }
    public string? PickupSiteName { get; set; }
    public string? Commodity { get; set; }

    /// <summary>The drops' delivery location names, in drop order.</summary>
    public List<string> DropNames { get; set; } = [];

    public decimal? EstimatedTotal { get; set; }
}

public class CreateTripFromBookingResultDto
{
    public Guid TripId { get; set; }

    /// <summary>The booking's selling total carried onto the trip.</summary>
    public decimal? PricedTotal { get; set; }

    /// <summary>Retained for compatibility with clients that expect pricing errors.</summary>
    public string? PricingError { get; set; }
}
