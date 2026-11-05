namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// The currency fields shared by the create and update commands, so both inherit one set of
/// validation rules.
/// </summary>
public interface ICurrencyFields
{
    string Name { get; }
    string? NumericCode { get; }
    string? Symbol { get; }
    int MinorUnits { get; }
    string? Notes { get; }
}
