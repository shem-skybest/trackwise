using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The rules that keep a container type describing a box that exists. Create and edit share one
/// base, so each rule is asserted through both - a rule that drifted to one side would let an edit
/// reach a state a create refuses.
/// </summary>
public class ContainerTypeCommandValidatorTests
{
    private static readonly CreateContainerTypeValidator Create = new();
    private static readonly UpdateContainerTypeValidator Update = new();

    private static CreateContainerTypeCommand NewCreate() => new()
    {
        Name = "40' High Cube",
        Category = ContainerTypeCategory.GeneralPurpose,
        LengthFeet = 40,
        IsHighCube = true,
        TeuFactor = 2m
    };

    private static UpdateContainerTypeCommand NewUpdate() => new()
    {
        Id = Guid.NewGuid(),
        Name = "40' High Cube",
        Category = ContainerTypeCategory.GeneralPurpose,
        LengthFeet = 40,
        IsHighCube = true,
        TeuFactor = 2m
    };

    [Fact]
    public void AValidTypeIsAccepted()
    {
        Assert.True(Create.Validate(NewCreate()).IsValid);
        Assert.True(Update.Validate(NewUpdate()).IsValid);
    }

    [Fact]
    public void ACodeIsOptionalOnCreateBecauseTheSeriesCanAllocateOne()
    {
        var command = NewCreate();
        command.Code = null;

        Assert.True(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("22G1")]
    [InlineData("22 G1")]
    [InlineData("22g1")]
    public void AnIsoCodeIsCheckedOnItsNormalisedForm(string iso)
    {
        // Paperwork writes the code with a space; rejecting that teaches stewards to strip it by
        // hand, which is how the value stops being reliable.
        var command = NewCreate();
        command.IsoSizeTypeCode = iso;

        Assert.True(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("22G")]
    [InlineData("22G12")]
    public void AnIsoCodeThatIsNotFourCharactersIsRejected(string iso)
    {
        var command = NewCreate();
        command.IsoSizeTypeCode = iso;

        Assert.False(Create.Validate(command).IsValid);

        var update = NewUpdate();
        update.IsoSizeTypeCode = iso;

        Assert.False(Update.Validate(update).IsValid);
    }

    [Fact]
    public void AnAbsentIsoCodeIsFineBecauseNonIsoEquipmentHasNone()
    {
        var command = NewCreate();
        command.IsoSizeTypeCode = null;
        command.LengthFeet = null;
        command.IsHighCube = false;
        command.TeuFactor = null;

        Assert.True(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(40)]
    [InlineData(45)]
    public void TheLengthsThatExistAreAccepted(int length)
    {
        var command = NewCreate();
        command.LengthFeet = length;
        command.IsHighCube = false;
        command.TeuFactor = null;

        Assert.True(Create.Validate(command).IsValid);
    }

    [Fact]
    public void ALengthNoChassisCarriesIsRejected()
    {
        var command = NewCreate();
        command.LengthFeet = 37;

        Assert.False(Create.Validate(command).IsValid);
    }

    [Fact]
    public void HighCubeNeedsALengthBecauseItDescribesABoxesHeight()
    {
        // Ticked on a road tanker it would read later as a fact somebody established.
        var command = NewCreate();
        command.LengthFeet = null;
        command.IsHighCube = true;
        command.TeuFactor = null;

        Assert.False(Create.Validate(command).IsValid);

        var update = NewUpdate();
        update.LengthFeet = null;
        update.IsHighCube = true;
        update.TeuFactor = null;

        Assert.False(Update.Validate(update).IsValid);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(5)]
    public void ATeuFactorOutsideTheRangeThatExistsIsRejected(double factor)
    {
        var command = NewCreate();
        command.TeuFactor = (decimal)factor;

        Assert.False(Create.Validate(command).IsValid);
    }

    [Fact]
    public void OverridingTheDuplicateProbeNeedsAReason()
    {
        var command = NewCreate();
        command.ConfirmedDistinct = true;

        Assert.False(Create.Validate(command).IsValid);

        command.DistinctReason = "A 40' high cube pallet-wide, which the existing record is not.";

        Assert.True(Create.Validate(command).IsValid);
    }

    [Fact]
    public void RetiringATypeThatIsStillInUseNeedsAReason()
    {
        var command = NewUpdate();
        command.IsActive = false;
        command.ConfirmedDespiteDependents = true;

        Assert.False(Update.Validate(command).IsValid);

        command.DependencyReason = "The remaining bookings were cancelled this morning.";

        Assert.True(Update.Validate(command).IsValid);
    }
}
