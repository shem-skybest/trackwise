using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payroll.Queries;

public class GetEmployeePayoutOnboardingLinkQuery : IQuery<Result<EmployeePayoutOnboardingLinkDto>>
{
    public Guid EmployeeId { get; set; }
    public required string ReturnUrl { get; set; }
    public required string RefreshUrl { get; set; }
}
