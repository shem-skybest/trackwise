namespace Trackwise.Shared.Models;

/// <summary>Where the Cold Chain booking number series (BKN-RFV) stands this year.</summary>
public class BookingNumberingDto
{
    /// <summary>
    /// Numbers are typed in from nSFS, which still issues them, rather than issued from the counter.
    /// </summary>
    public bool Typed { get; set; }

    /// <summary>The counter in use, e.g. "BKN-RFV-26-M" - a new one starts at 1 every year.</summary>
    public string CounterKey { get; set; } = default!;

    /// <summary>The highest number issued this year; 0 before the first.</summary>
    public long LastIssued { get; set; }

    public long NextNumber { get; set; }

    /// <summary>The number the next booking will get, e.g. "BKN-RFV1514-26-M".</summary>
    public string NextReference { get; set; } = default!;
}
