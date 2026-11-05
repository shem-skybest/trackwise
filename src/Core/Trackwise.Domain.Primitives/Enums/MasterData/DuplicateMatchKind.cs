namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// How a duplicate-probe rule compares a field.
/// </summary>
public enum DuplicateMatchKind
{
    /// <summary>
    /// Equality on a normalised identifier - code, TIN, plate, licence number. Scores 1.0 and is
    /// the strongest signal a record already exists.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Trigram similarity on a free-text name. Legacy data is full of "San Miguel Corp" against
    /// "San Miguel Corporation", which exact matching never catches.
    /// </summary>
    Fuzzy = 1
}
