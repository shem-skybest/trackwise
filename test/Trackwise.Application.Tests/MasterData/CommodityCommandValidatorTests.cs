using Trackwise.Application.Modules.MasterData.Commands;
using FluentValidation;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Commodity's own rules are about classification: an HS code that is shared by design, and a UN
/// number that only means something alongside the hazardous flag.
/// </summary>
public class CommodityCommandValidatorTests
{
    private static CreateCommodityCommand Valid(Action<CreateCommodityCommand>? tweak = null)
    {
        var command = new CreateCommodityCommand { Name = "Frozen Fish" };
        tweak?.Invoke(command);
        return command;
    }

    private static IEnumerable<string> Errors(CreateCommodityCommand command) =>
        new CreateCommodityValidator().Validate(command).Errors.Select(e => e.ErrorMessage);

    [Theory]
    [InlineData("030389")]
    [InlineData("0303.89")]
    [InlineData("0303.89.00.10")]
    public void AnHsCodeIsReadThroughItsNormalisedForm(string hs)
    {
        // Customs paperwork writes the code with dots. Six digits is the international heading;
        // countries subdivide to eight or ten.
        Assert.Empty(Errors(Valid(c => c.HsCode = hs)));
    }

    [Theory]
    [InlineData("0303")]
    [InlineData("03038900101")]
    public void AnHsCodeOfTheWrongLengthIsRefused(string hs)
    {
        Assert.Contains(
            "An HS code is between six and ten digits.", Errors(Valid(c => c.HsCode = hs)));
    }

    [Fact]
    public void AUnNumberWithoutTheHazardousFlagIsRefused()
    {
        // The two are one classification. A UN number on ordinary freight is a steward changing
        // their mind mid-form, and storing it would read later as a decision somebody made.
        Assert.Contains(
            "A UN number only applies to a hazardous commodity. Tick hazardous, or clear it.",
            Errors(Valid(c => c.UnNumber = "1263")));
    }

    [Fact]
    public void AHazardousCommodityAwaitingClassificationIsAccepted()
    {
        // One-directional on purpose: requiring the number would push the steward into inventing
        // one, which is worse than recording that the goods are hazardous and unclassified.
        Assert.Empty(Errors(Valid(c => c.IsHazardous = true)));
    }

    [Fact]
    public void AUnNumberThatIsNotFourDigitsIsRefused()
    {
        Assert.Contains(
            "A UN number is four digits.",
            Errors(Valid(c =>
            {
                c.IsHazardous = true;
                c.UnNumber = "126";
            })));
    }

    [Fact]
    public void OverridingTheDuplicateProbeNeedsAReason()
    {
        Assert.Contains(
            "Give a reason why this is not one of the existing commodities.",
            Errors(Valid(c => c.ConfirmedDistinct = true)));
    }
}
