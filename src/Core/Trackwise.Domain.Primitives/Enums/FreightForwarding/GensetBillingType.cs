namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// How a genset add-on is billed. Source: CCL genset billing shows two distinct shapes - itemized
/// hourly charges billed to the end client, or a flat cut (consistently ~PHP 2,000) when a partner
/// or co-loader fronts the job instead of Skybest billing the client directly.
/// </summary>
public enum GensetBillingType
{
    Itemized,
    ProfitShare
}
