using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Queries;

public class GetExpiringLicensesQuery : IQuery<Result<IList<DriverLicenseDto>>>
{
    /// <summary>
    /// Window in days. Returns active licenses expiring within this many days from now.
    /// </summary>
    public int WindowDays { get; set; } = 30;
}
