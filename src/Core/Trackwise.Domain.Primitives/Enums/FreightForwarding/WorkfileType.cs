namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// The three kinds of workfile. This is the TPH discriminator, not a label: each type carries
/// different fields, a different reference series, and different required-field rules.
/// </summary>
public enum WorkfileType
{
    Regular = 0,
    Reefer = 1,
    Trucking = 2
}
