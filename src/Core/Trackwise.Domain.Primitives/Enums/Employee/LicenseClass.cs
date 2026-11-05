using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Driver license class. US uses CDL classes A/B/C; EU uses categories C/CE/D/DE/C1/C1E. A
/// Philippine (LTO) license is Professional or Non-Professional, and what it allows the holder to
/// drive is listed separately as DL codes (<see cref="PhilippineDlCode" />).
/// </summary>
public enum LicenseClass
{
    [Description("US CDL Class A")]
    USClassA,

    [Description("US CDL Class B")]
    USClassB,

    [Description("US CDL Class C")]
    USClassC,

    [Description("EU Category C")]
    EUC,

    [Description("EU Category C+E")]
    EUCE,

    [Description("EU Category D")]
    EUD,

    [Description("EU Category D+E")]
    EUDE,

    [Description("EU Category C1")]
    EUC1,

    [Description("EU Category C1+E")]
    EUC1E,

    [Description("PH Professional")]
    PHProfessional,

    [Description("PH Non-Professional")]
    PHNonProfessional
}

public static class LicenseClassExtensions
{
    public static bool IsUS(this LicenseClass value) =>
        value is LicenseClass.USClassA or LicenseClass.USClassB or LicenseClass.USClassC;

    public static bool IsPhilippine(this LicenseClass value) =>
        value is LicenseClass.PHProfessional or LicenseClass.PHNonProfessional;

    /// <summary>
    /// US classes are only issued in the US and Philippine classes only in the Philippines; the EU
    /// categories cover every other issuing country.
    /// </summary>
    public static bool IsValidForCountry(this LicenseClass value, string? issuingCountry)
    {
        if (value.IsUS())
        {
            return issuingCountry == "US";
        }

        if (value.IsPhilippine())
        {
            return issuingCountry == "PH";
        }

        return issuingCountry is not ("US" or "PH");
    }
}
