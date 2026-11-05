namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// A foreign-key column that follows a merge to the survivor but never objects to a retirement -
/// a record's own history (a truck's expenses, DVIRs, papers). Declaring it as a
/// <see cref="DependencyRule" /> instead would block retiring the record over history that soft
/// delete already keeps.
/// </summary>
/// <param name="TableName">Physical table; interpolated into SQL, so a compile-time constant.</param>
/// <param name="Column">The column in <paramref name="TableName" /> pointing at the record.</param>
public sealed record MergeReference(string TableName, string Column);
