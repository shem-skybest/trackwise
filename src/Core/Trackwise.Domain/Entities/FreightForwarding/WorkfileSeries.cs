using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Maps a workfile's type (and its direction or variant) to its reference series.
/// <para>
/// Static because the series has to be known <em>before</em> the workfile exists: the reference is
/// allocated from the series, and the file is constructed with the reference. Each subclass's
/// <c>ReferenceSeries</c> delegates here so there is one source of truth.
/// </para>
/// </summary>
public static class WorkfileSeries
{
    public const string Reefer = "BKN-RFV";

    /// <summary>
    /// Cold Chain billing references, one per client billing period (<see cref="BillingBatch" />) -
    /// nSFS's SLC-RFV job numbers, numbered like BKN-RFV but on their own counter.
    /// </summary>
    public const string ReeferBilling = "SLC-RFV";

    /// <summary>
    /// Truck legs of a forwarding workfile (<see cref="TruckingWorkfile.IsLeg" />) - continues the
    /// nSFS SLC-TRK counter, whose INTERNAL jobs the legs replace.
    /// </summary>
    public const string TruckLeg = "SLC-TRK";

    /// <summary>
    /// The branch letter on nSFS-numbered references. nSFS numbers each branch on its own (M, O, D,
    /// C, E); workfiles and bookings don't carry a branch yet, and Cold Chain and almost all
    /// forwarding run from Manila, so every number is Manila's.
    /// </summary>
    public const string NsfsBranch = "M";

    /// <summary>The series numbered nSFS's way: per year and branch, printed "BKN-RFV1514-26-M".</summary>
    public static bool IsNsfsNumbered(string series) => series is Reefer or ReeferBilling or TruckLeg;

    public static string For(WorkfileType type, FreightMode? direction, TruckingVariant? variant) =>
        type switch
        {
            WorkfileType.Reefer => Reefer,
            WorkfileType.Trucking => ForTrucking(variant ?? TruckingVariant.Truck),
            _ => ForRegular(direction ?? FreightMode.Domestic)
        };

    public static string ForRegular(FreightMode direction) => direction switch
    {
        FreightMode.Import => "BKN-IMP",
        FreightMode.Export => "BKN-EXP",
        FreightMode.Triangular => "BKN-TRI",
        _ => "BKN-DOM"
    };

    public static string ForTrucking(TruckingVariant variant) =>
        variant is TruckingVariant.Trailer ? "BKN-TRL" : "BKN-TRK";
}
