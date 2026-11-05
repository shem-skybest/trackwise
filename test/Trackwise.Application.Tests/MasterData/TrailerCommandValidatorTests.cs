using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using FluentValidation;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The field rules a trailer has that a port does not: a VIN, which is the only globally unique
/// thing about it, and the numeric fields (year, axle count, load capacity) that mean nothing when
/// left blank.
/// </summary>
public class TrailerCommandValidatorTests
{
    private static CreateTrailerCommand Valid(Action<CreateTrailerCommand>? tweak = null)
    {
        var command = new CreateTrailerCommand
        {
            Name = "53' Dry Van #12",
            Type = TrailerType.DryVan
        };

        tweak?.Invoke(command);
        return command;
    }

    private static IEnumerable<string> Errors(CreateTrailerCommand command) =>
        new CreateTrailerValidator().Validate(command).Errors.Select(e => e.ErrorMessage);

    [Fact]
    public void ABlankCodeIsAccepted_BecauseItMeansAllocateOne()
    {
        Assert.Empty(Errors(Valid()));
    }

    [Fact]
    public void ASeventeenCharacterVinIsAccepted()
    {
        Assert.Empty(Errors(Valid(c => c.Vin = "1GRAA0620XB123456")));
    }

    [Theory]
    [InlineData("1GR")]
    [InlineData("1GRAA0620XB1234567890")]
    public void AVinThatIsNotSeventeenCharactersIsRefused(string vin)
    {
        Assert.Contains("A VIN is 17 characters.", Errors(Valid(c => c.Vin = vin)));
    }

    [Fact]
    public void NoVinIsFine_BecauseNotEveryTrailerHasOneOnFile()
    {
        Assert.Empty(Errors(Valid(c => c.Vin = "  ")));
    }

    [Fact]
    public void AZeroAxleCountIsRefused()
    {
        Assert.NotEmpty(Errors(Valid(c => c.AxleCount = 0)));
    }

    [Fact]
    public void AZeroMaxLoadCapacityIsRefused()
    {
        Assert.NotEmpty(Errors(Valid(c => c.MaxLoadCapacityKg = 0)));
    }

    [Fact]
    public void AYearOutsideTheSaneRangeIsRefused()
    {
        Assert.NotEmpty(Errors(Valid(c => c.Year = 1900)));
    }

    [Fact]
    public void OverridingTheDuplicateProbeNeedsAReason()
    {
        Assert.Contains(
            "Give a reason why this is not one of the existing trailers.",
            Errors(Valid(c => c.ConfirmedDistinct = true)));
    }

    [Fact]
    public void DeactivatingPastDependentsNeedsAReason()
    {
        var command = new UpdateTrailerCommand
        {
            Id = Guid.NewGuid(),
            Name = "53' Dry Van #12",
            Type = TrailerType.DryVan,
            IsActive = false,
            ConfirmedDespiteDependents = true
        };

        Assert.Contains(
            "Give a reason for deactivating a trailer that is still in use.",
            new UpdateTrailerValidator().Validate(command).Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void AnEditThatLeavesTheTrailerActiveNeedsNoReason()
    {
        var command = new UpdateTrailerCommand
        {
            Id = Guid.NewGuid(),
            Name = "53' Dry Van #12",
            Type = TrailerType.DryVan,
            IsActive = true,
            ConfirmedDespiteDependents = true
        };

        Assert.True(new UpdateTrailerValidator().Validate(command).IsValid);
    }
}
