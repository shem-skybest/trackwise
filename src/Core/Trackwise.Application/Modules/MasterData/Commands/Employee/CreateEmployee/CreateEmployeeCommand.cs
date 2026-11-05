using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Turns an existing platform <c>User</c> into an employee of this tenant. Unlike every other
/// master-data create command, this one does not build a brand-new record from caller-supplied
/// fields: <c>Employee.Id</c> is the <c>User.Id</c> by convention (see
/// <c>Employee.CreateEmployeeFromUser</c>), so "create" here means "attach", and <c>Code</c> is
/// always server-allocated since nothing supplies one.
/// <para>
/// Deliberately NOT <c>IMasterDataWriteCommand</c>: the duplicate probe matches on <c>Name</c>, but
/// this command carries no <c>FirstName</c>/<c>LastName</c> - those live on the <c>User</c> row this
/// command references, unresolved until the handler runs, past the point the write pipeline reads
/// <c>DedupeFields</c>. The existing-employee check below is what this path relies on instead: a
/// given <c>UserId</c> can only ever be attached once.
/// </para>
/// <para>
/// Separate from <c>AcceptInvitationHandler</c>'s onboarding path, which also calls
/// <c>Employee.CreateEmployeeFromUser</c> - that flow is untouched by this promotion.
/// </para>
/// </summary>
public class CreateEmployeeCommand : ICommand<Result<EmployeeDto>>
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public decimal Salary { get; set; }
    public SalaryType SalaryType { get; set; }

    /// <summary>Owning branch, or null when every branch uses this employee record.</summary>
    public Guid? BranchId { get; set; }
}
