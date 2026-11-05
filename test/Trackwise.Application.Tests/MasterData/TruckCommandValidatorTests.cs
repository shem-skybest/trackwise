using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Truck is the module's first promotion of an entity with an existing life, so the parts worth
/// pinning are the ones a mechanical copy of Port's validator would get wrong: the unit number stays
/// optional and unbounded by the UN/LOCODE shape, and both duplicate-probe and dependency overrides
/// still cost a reason.
/// </summary>
public class TruckCommandValidatorTests
{
    #region Create

    private readonly CreateTruckValidator create = new();

    private static CreateTruckCommand ValidCreate() => new()
    {
        Number = "TRK-042",
        Name = "2022 Freightliner Cascadia",
        Type = TruckType.FreightTruck,
        VehicleCapacity = 1
    };

    [Fact]
    public void Create_ValidCommand_Passes()
    {
        Assert.True(create.Validate(ValidCreate()).IsValid);
    }

    [Fact]
    public void Create_WithoutANumber_Passes()
    {
        // Blank means "allocate one from the series" - a spare unit can be registered before it is
        // assigned a call sign.
        var command = ValidCreate();
        command.Number = null;

        Assert.True(create.Validate(command).IsValid);
    }

    [Fact]
    public void Create_OverridingTheDuplicateProbeWithoutAReason_Fails()
    {
        var command = ValidCreate();
        command.ConfirmedDistinct = true;

        Assert.False(create.Validate(command).IsValid);
    }

    [Fact]
    public void Create_OverridingTheDuplicateProbeWithAReason_Passes()
    {
        var command = ValidCreate();
        command.ConfirmedDistinct = true;
        command.DistinctReason = "Second unit bought from the same dealer, different VIN.";

        Assert.True(create.Validate(command).IsValid);
    }

    #endregion

    #region Update

    private readonly UpdateTruckValidator update = new();

    private static UpdateTruckCommand ValidUpdate() => new()
    {
        Id = Guid.NewGuid(),
        Name = "2022 Freightliner Cascadia",
        Type = TruckType.FreightTruck,
        Status = TruckStatus.Available
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
    public void Update_ThatLeavesTheTruckActive_NeedsNoDependencyReason()
    {
        // An edit is not a retirement. Demanding a reason here would train stewards to type one
        // into every save, which is how the reason stops meaning anything.
        var command = ValidUpdate();
        command.ConfirmedDespiteDependents = true;

        Assert.True(update.Validate(command).IsValid);
    }

    #endregion

    #region Delete

    private readonly DeleteTruckValidator delete = new();

    [Fact]
    public void Delete_OverridingDependentsWithoutAReason_Fails()
    {
        var command = new DeleteTruckCommand
        {
            Id = Guid.NewGuid(),
            ConfirmedDespiteDependents = true
        };

        Assert.False(delete.Validate(command).IsValid);
    }

    #endregion

    #region Merge

    private readonly MergeTrucksValidator merge = new();

    private static MergeTrucksCommand ValidMerge() => new()
    {
        SurvivorId = Guid.NewGuid(),
        LoserId = Guid.NewGuid(),
        Reason = "Same unit entered twice under two call signs."
    };

    [Fact]
    public void Merge_ValidCommand_Passes()
    {
        Assert.True(merge.Validate(ValidMerge()).IsValid);
    }

    [Fact]
    public void Merge_TakingTheVinFromTheLoser_Fails()
    {
        // The VIN is a second exact rule, not a mergeable field: two different VINs mean two real
        // vehicles, so the merge itself would already have been refused before a field choice matters.
        var command = ValidMerge();
        command.FieldChoices["Vin"] = "loser";

        Assert.False(merge.Validate(command).IsValid);
    }

    [Fact]
    public void Merge_TakingTheBranchFromTheLoser_Passes()
    {
        // Branch scoping is ordinary data: the duplicate often holds the correct owning office.
        var command = ValidMerge();
        command.FieldChoices["BranchId"] = "loser";

        Assert.True(merge.Validate(command).IsValid);
    }

    #endregion
}
