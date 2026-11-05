using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// LTO driver's license codes: the vehicle categories printed on a Philippine license. One license
/// lists several (e.g. "B, B1, B2, C"), so this is a flags value like <see cref="LicenseEndorsement" />.
/// Gross vehicle weight (GVW) decides between the goods-vehicle codes.
/// </summary>
[Flags]
public enum PhilippineDlCode
{
    None = 0,

    [Description("A - Motorcycle")]
    A = 1 << 0,

    [Description("A1 - Tricycle")]
    A1 = 1 << 1,

    [Description("B - Car, up to 5,000 kg GVW, up to 8 seats")]
    B = 1 << 2,

    [Description("B1 - Van, up to 5,000 kg GVW, 9+ seats")]
    B1 = 1 << 3,

    [Description("B2 - Goods vehicle up to 3,500 kg GVW")]
    B2 = 1 << 4,

    [Description("C - Goods vehicle over 3,500 kg GVW")]
    C = 1 << 5,

    [Description("D - Bus over 5,000 kg GVW, 9+ seats")]
    D = 1 << 6,

    [Description("BE - B/B1/B2 with trailer over 750 kg")]
    BE = 1 << 7,

    [Description("CE - C with trailer over 750 kg (articulated)")]
    CE = 1 << 8
}

public static class PhilippineDlCodeExtensions
{
    private static readonly PhilippineDlCode[] AllValues =
    [
        PhilippineDlCode.A,
        PhilippineDlCode.A1,
        PhilippineDlCode.B,
        PhilippineDlCode.B1,
        PhilippineDlCode.B2,
        PhilippineDlCode.C,
        PhilippineDlCode.D,
        PhilippineDlCode.BE,
        PhilippineDlCode.CE
    ];

    public static PhilippineDlCode[] ToArray(this PhilippineDlCode value) =>
        AllValues.Where(v => value.HasFlag(v)).ToArray();

    public static PhilippineDlCode Combine(this IEnumerable<PhilippineDlCode>? values) =>
        values?.Aggregate(PhilippineDlCode.None, (acc, v) => acc | v) ?? PhilippineDlCode.None;
}
