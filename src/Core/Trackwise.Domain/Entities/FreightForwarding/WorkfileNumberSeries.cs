using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Per-series counter behind workfile reference numbers. Booking gets away with an identity column
/// because it has one sequence; a workfile draws from one of six (BKN-IMP, BKN-RFV, ...), each
/// numbered independently, so the counters are rows.
/// <para>
/// Allocation must be a single atomic statement, never read-then-write - two files opened in the
/// same second would otherwise take the same number, and the unique index on
/// <c>Workfile.ReferenceNumber</c> would fail the second one at save time.
/// </para>
/// </summary>
public class WorkfileNumberSeries : Entity, ITenantEntity
{
    /// <summary>The counter's key - see <see cref="CounterKey" />.</summary>
    public required string Series { get; set; }

    /// <summary>
    /// The highest number handed out so far; 0 before the first allocation. Stored as "last" rather
    /// than "next" so the allocating statement can simply return the row's new value.
    /// </summary>
    public long LastAllocated { get; set; }

    /// <summary>
    /// The counter a series draws from on <paramref name="issuedOn" /> (the tenant's local day).
    /// BKN-RFV, SLC-RFV and SLC-TRK continue Skybest's nSFS numbering, which starts over at 1 every
    /// year per branch, so each has a counter per year and branch ("BKN-RFV-26-M", nSFS's
    /// <c>TBL_SERIES</c> row); the other series run one counter forever.
    /// </summary>
    public static string CounterKey(string series, DateOnly issuedOn) =>
        WorkfileSeries.IsNsfsNumbered(series)
            ? $"{series}-{YearSuffix(issuedOn)}-{WorkfileSeries.NsfsBranch}"
            : series;

    /// <summary>
    /// Formats an allocated number into its display reference: "BKN-RFV1514-26-M" for the
    /// nSFS-numbered series, as nSFS prints it (the year is the day the number is issued, not the
    /// service date), and "BKN-IMP-000123" for the rest.
    /// </summary>
    public static string FormatReference(string series, DateOnly issuedOn, long value) =>
        WorkfileSeries.IsNsfsNumbered(series)
            ? $"{series}{value:D4}-{YearSuffix(issuedOn)}-{WorkfileSeries.NsfsBranch}"
            : $"{series}-{value:D6}";

    private static string YearSuffix(DateOnly day) => (day.Year % 100).ToString("D2");
}
