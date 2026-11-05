using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using FluentValidation;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The field rules a vessel has that a port does not: an IMO number, which is the only globally
/// unique thing about a ship, and a TEU figure that means nothing on most of the fleet.
/// </summary>
public class VesselCommandValidatorTests
{
    private static CreateVesselCommand Valid(Action<CreateVesselCommand>? tweak = null)
    {
        var command = new CreateVesselCommand
        {
            Name = "Ever Given",
            Type = VesselType.ContainerShip
        };

        tweak?.Invoke(command);
        return command;
    }

    private static IEnumerable<string> Errors(CreateVesselCommand command) =>
        new CreateVesselValidator().Validate(command).Errors.Select(e => e.ErrorMessage);

    [Fact]
    public void ABlankCodeIsAccepted_BecauseItMeansAllocateOne()
    {
        Assert.Empty(Errors(Valid()));
    }

    [Theory]
    [InlineData("9811000")]
    [InlineData("IMO 9811000")]
    [InlineData("IMO9811000")]
    [InlineData("981-1000")]
    public void AnImoNumberIsReadThroughItsNormalisedForm(string imo)
    {
        // A steward pasting the number off a manifest is entering a valid one. Rejecting it for the
        // prefix would teach them to strip it by hand on every vessel.
        Assert.Empty(Errors(Valid(c => c.ImoNumber = imo)));
    }

    [Theory]
    [InlineData("981100")]
    [InlineData("98110000")]
    [InlineData("IMO")]
    public void AnImoNumberThatIsNotSevenDigitsIsRefused(string imo)
    {
        Assert.Contains("An IMO number is seven digits.", Errors(Valid(c => c.ImoNumber = imo)));
    }

    [Fact]
    public void NoImoNumberIsFine_BecauseMostOfTheDomesticFleetHasNone()
    {
        Assert.Empty(Errors(Valid(c => c.ImoNumber = "  ")));
    }

    [Fact]
    public void ATeuCapacityOfZeroIsRefusedRatherThanStored()
    {
        // Zero reads as "this ship carries no containers", which is what an empty capacity already
        // says properly for a tanker.
        Assert.Contains(
            "Leave TEU capacity empty rather than zero when it does not apply.",
            Errors(Valid(c => c.TeuCapacity = 0)));
    }

    [Fact]
    public void OverridingTheDuplicateProbeNeedsAReason()
    {
        // The reason is what a reviewer reads months later when the duplicate turns out to be real.
        Assert.Contains(
            "Give a reason why this is not one of the existing vessels.",
            Errors(Valid(c => c.ConfirmedDistinct = true)));
    }

    [Fact]
    public void DeactivatingPastDependentsNeedsAReason()
    {
        var command = new UpdateVesselCommand
        {
            Id = Guid.NewGuid(),
            Name = "Ever Given",
            Type = VesselType.ContainerShip,
            IsActive = false,
            ConfirmedDespiteDependents = true
        };

        Assert.Contains(
            "Give a reason for deactivating a vessel that is still in use.",
            new UpdateVesselValidator().Validate(command).Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void AnEditThatLeavesTheVesselActiveNeedsNoReason()
    {
        // Only clearing the flag is a retirement. Demanding a reason for an ordinary edit would
        // train stewards to type one.
        var command = new UpdateVesselCommand
        {
            Id = Guid.NewGuid(),
            Name = "Ever Given",
            Type = VesselType.ContainerShip,
            IsActive = true,
            ConfirmedDespiteDependents = true
        };

        Assert.True(new UpdateVesselValidator().Validate(command).IsValid);
    }
}
