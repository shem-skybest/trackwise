namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// The box-shape fields shared by the create and update commands, so both inherit one set of
/// validation rules.
/// </summary>
public interface IContainerTypeFields
{
    string? IsoSizeTypeCode { get; }
    int? LengthFeet { get; }
    bool IsHighCube { get; }
    decimal? TeuFactor { get; }
}
