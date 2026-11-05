namespace Trackwise.Infrastructure.Persistence;

/// <summary>
///     Local connection strings used for development (e.g., creating migrations).
/// </summary>
internal static class ConnectionStrings
{
    public const string LocalMaster =
        "Host=localhost; Port=5433; Database=master_trackwise; Username=postgres; Password=Test12345#";

    public const string LocalDefaultTenant =
        "Host=localhost; Port=5433; Database=default_trackwise; Username=postgres; Password=Test12345#";
}
