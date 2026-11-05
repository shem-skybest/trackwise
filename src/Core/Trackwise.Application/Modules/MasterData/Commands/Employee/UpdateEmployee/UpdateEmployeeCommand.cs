using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits an employee. Carries no <c>Code</c> - it is the handle other systems join on, so a rename
/// would strand every legacy key pointing at this record.
/// </summary>
public class UpdateEmployeeCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    /// <summary>Null leaves the name unchanged. Recomputes <see cref="EmployeeDto.FullName"/>.</summary>
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Role { get; set; }

    /// <summary>Null leaves it unchanged; an empty string clears it.</summary>
    public string? JobTitle { get; set; }

    public decimal? Salary { get; set; }
    public SalaryType? SalaryType { get; set; }
    public EmployeeStatus? Status { get; set; }

    /// <summary>For drivers: regular or on-call. Null leaves it unchanged.</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>For drivers: the unit they normally work for. Null leaves it unchanged.</summary>
    public CrewUnit? HomeUnit { get; set; }
    public Address? Address { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every load, license and payroll entry that references it;
    /// it only stops the employee being offered anywhere a steward picks staff from.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this employee and is deactivating it
    /// anyway. Requires <see cref="DependencyReason"/>. Ignored when the employee stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Employee);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
