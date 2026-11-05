using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Ports are the first master entity with real dependents, so the overrides that let a steward
/// write anyway are the part worth pinning: each one is allowed, and each one costs a reason.
/// </summary>
public class PortCommandValidatorTests
{
    private static Address ValidAddress() => new()
    {
        StreetAddress = "Eva Macapagal Super Terminal",
        CityOrMunicipality = "Manila",
        Province = "Metro Manila",
        Region = "NCR",
        Zipcode = "1018",
        Country = "PH"
    };

    #region Create

    private readonly CreatePortValidator create = new();

    private static CreatePortCommand ValidCreate() => new()
    {
        Name = "Manila International Container Terminal",
        CountryCode = "PH",
        Type = TerminalType.SeaPort,
        Address = ValidAddress()
    };

    [Fact]
    public void Create_ValidCommand_Passes()
    {
        Assert.True(create.Validate(ValidCreate()).IsValid);
    }

    [Fact]
    public void Create_WithoutACode_Passes()
    {
        // Blank means "allocate one from the series". Inland depots and border crossings routinely
        // have no UN/LOCODE, so requiring one would push stewards into inventing them.
        var command = ValidCreate();
        command.Code = null;

        Assert.True(create.Validate(command).IsValid);
    }

    [Fact]
    public void Create_WithALongerThanLocodeCode_Passes()
    {
        // Deliberately not the five-character rule the old terminal validator enforced: the legacy
        // port rows this table absorbs carry codes that were never UN/LOCODEs.
        var command = ValidCreate();
        command.Code = "PH-MNL-SOUTH-HARBOUR";

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
        command.DistinctReason = "Separate berth operated by a different terminal company.";

        Assert.True(create.Validate(command).IsValid);
    }

    #endregion

    #region Update

    private readonly UpdatePortValidator update = new();

    private static UpdatePortCommand ValidUpdate() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Manila International Container Terminal",
        CountryCode = "PH",
        Type = TerminalType.SeaPort,
        Address = ValidAddress()
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
    public void Update_ThatLeavesThePortActive_NeedsNoDependencyReason()
    {
        // An edit is not a retirement. Demanding a reason here would train stewards to type one
        // into every save, which is how the reason stops meaning anything.
        var command = ValidUpdate();
        command.ConfirmedDespiteDependents = true;

        Assert.True(update.Validate(command).IsValid);
    }

    #endregion

    #region Delete

    private readonly DeletePortValidator delete = new();

    [Fact]
    public void Delete_OverridingDependentsWithoutAReason_Fails()
    {
        var command = new DeletePortCommand
        {
            Id = Guid.NewGuid(),
            ConfirmedDespiteDependents = true
        };

        Assert.False(delete.Validate(command).IsValid);
    }

    #endregion

    #region Merge

    private readonly MergePortsValidator merge = new();

    private static MergePortsCommand ValidMerge() => new()
    {
        SurvivorId = Guid.NewGuid(),
        LoserId = Guid.NewGuid(),
        Reason = "Same port entered twice, once abbreviated."
    };

    [Fact]
    public void Merge_ValidCommand_Passes()
    {
        Assert.True(merge.Validate(ValidMerge()).IsValid);
    }

    [Fact]
    public void Merge_TakingTheCodeFromTheLoser_Fails()
    {
        // The code is the handle other systems join on and it survives the merge, so it is not in
        // the mergeable set - taking the loser's would strand every legacy key on the survivor.
        var command = ValidMerge();
        command.FieldChoices["Code"] = "loser";

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
