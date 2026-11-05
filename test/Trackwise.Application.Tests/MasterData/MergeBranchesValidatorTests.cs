using Trackwise.Application.Modules.MasterData.Commands;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// A merge is the hardest master-data action to undo, so the guards on what may be merged and what
/// must be recorded are worth pinning.
/// </summary>
public class MergeBranchesValidatorTests
{
    private readonly MergeBranchesValidator sut = new();

    private static MergeBranchesCommand Valid() => new()
    {
        SurvivorId = Guid.NewGuid(),
        LoserId = Guid.NewGuid(),
        Reason = "Same depot created twice."
    };

    [Fact]
    public void ValidCommand_Passes()
    {
        Assert.True(sut.Validate(Valid()).IsValid);
    }

    [Fact]
    public void MergingARecordIntoItself_Fails()
    {
        var id = Guid.NewGuid();
        var command = Valid();
        command.SurvivorId = id;
        command.LoserId = id;

        Assert.False(sut.Validate(command).IsValid);
    }

    [Fact]
    public void MissingReason_Fails()
    {
        // The reason is what a reviewer reads when a merge turns out to have been wrong, so it is
        // required rather than optional.
        var command = Valid();
        command.Reason = null;

        Assert.False(sut.Validate(command).IsValid);
    }

    [Fact]
    public void FieldChoice_MustNameAMergeableField()
    {
        var command = Valid();

        // Code is deliberately absent from the mergeable set: it is the handle other systems join
        // on, and the loser's keeps resolving through the cross-reference instead.
        command.FieldChoices = new Dictionary<string, string> { ["Code"] = "loser" };

        Assert.False(sut.Validate(command).IsValid);
    }

    [Fact]
    public void FieldChoice_MustNameASide()
    {
        var command = Valid();
        command.FieldChoices = new Dictionary<string, string> { ["TaxId"] = "newest" };

        // No automatic "newest wins" - the steward resolves each conflict explicitly.
        Assert.False(sut.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("survivor")]
    [InlineData("loser")]
    public void FieldChoice_AcceptsEitherSide(string side)
    {
        var command = Valid();
        command.FieldChoices = new Dictionary<string, string> { ["TaxId"] = side };

        Assert.True(sut.Validate(command).IsValid);
    }
}
