namespace Trackwise.Infrastructure.Persistence.Options;

public record TenantDbContextOptions
{
    /// <summary>
    /// ConnectionStrings entry used for the runtime non-HTTP fallback (background jobs,
    /// IDataProtector, etc.). Defaults to <c>PHTenantDatabase</c> - the PH demo tenant.
    /// </summary>
    public string DefaultTenantDbConnectionSection { get; set; } = "PHTenantDatabase";
    public string? ConnectionString { get; set; }
}
