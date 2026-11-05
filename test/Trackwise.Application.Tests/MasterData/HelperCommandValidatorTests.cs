using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.ValueObjects;
using FluentValidation;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// A helper carries no type or second identifier, so the only field rules beyond the shared shape
/// are contact details and the optional address block.
/// </summary>
public class HelperCommandValidatorTests
{
    private static CreateHelperCommand Valid(Action<CreateHelperCommand>? tweak = null)
    {
        var command = new CreateHelperCommand
        {
            Name = "Juan Dela Cruz"
        };

        tweak?.Invoke(command);
        return command;
    }

    private static IEnumerable<string> Errors(CreateHelperCommand command) =>
        new CreateHelperValidator().Validate(command).Errors.Select(e => e.ErrorMessage);

    [Fact]
    public void ABlankCodeIsAccepted_BecauseItMeansAllocateOne()
    {
        Assert.Empty(Errors(Valid()));
    }

    [Fact]
    public void NoAddressIsFine_BecauseNotEveryHelperHasOneOnFile()
    {
        Assert.Empty(Errors(Valid()));
    }

    [Fact]
    public void APartialAddressNamesWhatIsMissing()
    {
        var errors = Errors(Valid(c => c.Address = new Address
        {
            StreetAddress = "123 Rizal St",
            CityOrMunicipality = "",
            Province = "",
            Region = "",
            Zipcode = "",
            Country = "PH"
        }));

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void OverridingTheDuplicateProbeNeedsAReason()
    {
        Assert.Contains(
            "Give a reason why this is not one of the existing helpers.",
            Errors(Valid(c => c.ConfirmedDistinct = true)));
    }

    [Fact]
    public void DeactivatingPastDependentsNeedsAReason()
    {
        var command = new UpdateHelperCommand
        {
            Id = Guid.NewGuid(),
            Name = "Juan Dela Cruz",
            IsActive = false,
            ConfirmedDespiteDependents = true
        };

        Assert.Contains(
            "Give a reason for deactivating a helper that is still in use.",
            new UpdateHelperValidator().Validate(command).Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void AnEditThatLeavesTheHelperActiveNeedsNoReason()
    {
        var command = new UpdateHelperCommand
        {
            Id = Guid.NewGuid(),
            Name = "Juan Dela Cruz",
            IsActive = true,
            ConfirmedDespiteDependents = true
        };

        Assert.True(new UpdateHelperValidator().Validate(command).IsValid);
    }
}
