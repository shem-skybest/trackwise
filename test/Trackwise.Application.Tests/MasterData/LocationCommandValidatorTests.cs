using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using FluentValidation;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Location's rules are about the one thing that makes it different from every other reference
/// list: a zone is a grouping rather than a place, so it has no address, and it can contain other
/// locations - including, if nothing stops it, itself.
/// </summary>
public class LocationCommandValidatorTests
{
    private static Address Somewhere() => new()
    {
        StreetAddress = "Gov. Drive",
        CityOrMunicipality = "Rosario",
        Province = "Cavite",
        Region = "CALABARZON",
        Zipcode = "4106",
        Country = "PH"
    };

    private static CreateLocationCommand Valid(Action<CreateLocationCommand>? tweak = null)
    {
        var command = new CreateLocationCommand
        {
            Name = "Cavite Export Processing Zone",
            Type = LocationType.DeliveryPoint,
            Address = Somewhere()
        };

        tweak?.Invoke(command);
        return command;
    }

    private static IEnumerable<string> Errors(CreateLocationCommand command) =>
        new CreateLocationValidator().Validate(command).Errors.Select(e => e.ErrorMessage);

    [Fact]
    public void ADeliveryPointNeedsAnAddress()
    {
        Assert.Contains(
            "A location needs an address. Only a zone can be left without one.",
            Errors(Valid(c => c.Address = null)));
    }

    [Fact]
    public void AZoneIsRefusedAnAddress()
    {
        // A zone carrying one of its members' addresses is how a zone quietly becomes a duplicate
        // delivery point.
        Assert.Contains(
            "A zone groups other locations and has no address of its own.",
            Errors(Valid(c => c.Type = LocationType.Zone)));
    }

    [Fact]
    public void AZoneWithoutAnAddressIsAccepted()
    {
        Assert.Empty(Errors(Valid(c =>
        {
            c.Type = LocationType.Zone;
            c.Address = null;
        })));
    }

    [Fact]
    public void HalfACoordinateIsRefused()
    {
        // One half of a pair points at the Gulf of Guinea, which is where every half-filled
        // coordinate on a map ends up.
        Assert.Contains(
            "Give both latitude and longitude, or neither.",
            Errors(Valid(c => c.Latitude = 14.4167)));
    }

    [Fact]
    public void BothCoordinatesTogetherAreAccepted()
    {
        Assert.Empty(Errors(Valid(c =>
        {
            c.Latitude = 14.4167;
            c.Longitude = 120.85;
        })));
    }

    [Fact]
    public void ALocationCannotSitInsideItself()
    {
        // The one cycle that needs no database walk. The deeper loops are LocationAncestry's.
        var id = Guid.NewGuid();

        var command = new UpdateLocationCommand
        {
            Id = id,
            Name = "Cavite",
            Type = LocationType.DeliveryPoint,
            Address = Somewhere(),
            ParentLocationId = id
        };

        Assert.Contains(
            "A location cannot sit inside itself.",
            new UpdateLocationValidator().Validate(command).Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void OverridingTheDuplicateProbeNeedsAReason()
    {
        Assert.Contains(
            "Give a reason why this is not one of the existing locations.",
            Errors(Valid(c => c.ConfirmedDistinct = true)));
    }
}
