using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Employee is the hardest promotion in the module: its id is a live platform login, so Delete and
/// Merge carry a second confirmation gate beyond the ordinary dependency-in-use override - these pin
/// that both gates require their own reason, and that an ordinary edit or merge does not accidentally
/// demand one.
/// </summary>
public class EmployeeCommandValidatorTests
{
    #region Create

    private readonly CreateEmployeeValidator create = new();

    private static CreateEmployeeCommand ValidCreate() => new()
    {
        UserId = Guid.NewGuid(),
        Salary = 0,
        SalaryType = SalaryType.None
    };

    [Fact]
    public void Create_ValidCommand_Passes()
    {
        Assert.True(create.Validate(ValidCreate()).IsValid);
    }

    [Fact]
    public void Create_WithoutAUserId_Fails()
    {
        // There is no "allocate one from the series" escape hatch here - attaching a record with no
        // User to attach to is meaningless, unlike every other master entity's optional code.
        var command = ValidCreate();
        command.UserId = Guid.Empty;

        Assert.False(create.Validate(command).IsValid);
    }

    [Fact]
    public void Create_WithASalaryTypeButNoSalary_Fails()
    {
        var command = ValidCreate();
        command.SalaryType = SalaryType.Monthly;
        command.Salary = 0;

        Assert.False(create.Validate(command).IsValid);
    }

    #endregion

    #region Update

    private readonly UpdateEmployeeValidator update = new();

    private static UpdateEmployeeCommand ValidUpdate() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Juan",
        LastName = "Dela Cruz"
    };

    [Fact]
    public void Update_ValidCommand_Passes()
    {
        Assert.True(update.Validate(ValidUpdate()).IsValid);
    }

    [Fact]
    public void Update_DeactivatingDespiteDependentsWithoutAReason_Fails()
    {
        var command = ValidUpdate();
        command.IsActive = false;
        command.ConfirmedDespiteDependents = true;

        Assert.False(update.Validate(command).IsValid);
    }

    [Fact]
    public void Update_ThatLeavesTheEmployeeActive_NeedsNoDependencyReason()
    {
        // An edit is not an offboarding. Demanding a reason here would train stewards to type one
        // into every save, which is how the reason stops meaning anything.
        var command = ValidUpdate();
        command.ConfirmedDespiteDependents = true;

        Assert.True(update.Validate(command).IsValid);
    }

    #endregion

    #region Delete

    private readonly DeleteEmployeeValidator delete = new();

    [Fact]
    public void Delete_OverridingDependentsWithoutAReason_Fails()
    {
        // Covers both the ordinary dependency-in-use override and the live-login guard - the
        // validator cannot tell which one triggered ConfirmedDespiteDependents, so it demands a
        // reason for either.
        var command = new DeleteEmployeeCommand
        {
            Id = Guid.NewGuid(),
            ConfirmedDespiteDependents = true
        };

        Assert.False(delete.Validate(command).IsValid);
    }

    [Fact]
    public void Delete_OverridingDependentsWithAReason_Passes()
    {
        var command = new DeleteEmployeeCommand
        {
            Id = Guid.NewGuid(),
            ConfirmedDespiteDependents = true,
            DependencyReason = "Duplicate record cleanup; the live login belongs on the survivor."
        };

        Assert.True(delete.Validate(command).IsValid);
    }

    #endregion

    #region Merge

    private readonly MergeEmployeesValidator merge = new();

    private static MergeEmployeesCommand ValidMerge() => new()
    {
        SurvivorId = Guid.NewGuid(),
        LoserId = Guid.NewGuid(),
        Reason = "Same person entered twice under two spellings of their name."
    };

    [Fact]
    public void Merge_ValidCommand_Passes()
    {
        Assert.True(merge.Validate(ValidMerge()).IsValid);
    }

    [Fact]
    public void Merge_TakingSalaryFromTheLoser_Fails()
    {
        // Payroll fields are deliberately not mergeable - a data-hygiene merge must never silently
        // change what someone is paid.
        var command = ValidMerge();
        command.FieldChoices["Salary"] = "loser";

        Assert.False(merge.Validate(command).IsValid);
    }

    [Fact]
    public void Merge_TakingTheBranchFromTheLoser_Passes()
    {
        var command = ValidMerge();
        command.FieldChoices["BranchId"] = "loser";

        Assert.True(merge.Validate(command).IsValid);
    }

    [Fact]
    public void Merge_OfALiveLoginWithoutAReason_Fails()
    {
        var command = ValidMerge();
        command.ConfirmedDespiteDependents = true;

        Assert.False(merge.Validate(command).IsValid);
    }

    [Fact]
    public void Merge_OfALiveLoginWithAReason_Passes()
    {
        var command = ValidMerge();
        command.ConfirmedDespiteDependents = true;
        command.DependencyReason = "Confirmed with the employee; consolidating their duplicate record.";

        Assert.True(merge.Validate(command).IsValid);
    }

    #endregion
}
