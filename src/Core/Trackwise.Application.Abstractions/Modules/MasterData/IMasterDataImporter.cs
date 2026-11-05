namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// Loads a file of master records through the same write pipeline a steward's form uses.
/// <para>
/// Two guarantees define it. A <b>dry run</b> is the real import rolled back, not a cheaper
/// imitation of it, so what the rehearsal reports is what the commit does. And an import is
/// <b>all-or-nothing</b>: 430 rows land together or none do, because a partially loaded reference
/// table is the one outcome nobody can recover from by hand - the failed rows are indistinguishable
/// from the ones nobody meant to send.
/// </para>
/// </summary>
public interface IMasterDataImporter
{
    Task<MasterDataImportReport> ImportAsync(
        MasterDataImportRequest request, CancellationToken ct = default);

    /// <summary>
    /// A file with the entity's headers and one sample row. The steward's starting point, and the
    /// only place the expected column names are ever written down for them.
    /// </summary>
    /// <returns>CSV text, or null when no entity of that name is importable.</returns>
    string? BuildTemplate(string entityType);
}
