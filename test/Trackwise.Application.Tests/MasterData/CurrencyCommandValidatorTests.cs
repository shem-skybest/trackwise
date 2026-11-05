using Trackwise.Application.Modules.MasterData.Commands;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Currency is the one entity in the module whose code is <b>required</b> - ISO 4217 assigns it, so
/// there is nothing for the number series to allocate. These pin that, and the minor-units range,
/// which silently decides how every total in the currency rounds.
/// </summary>
public class CurrencyCommandValidatorTests
{
    private static readonly CreateCurrencyValidator Create = new();
    private static readonly UpdateCurrencyValidator Update = new();

    private static CreateCurrencyCommand NewCreate() => new()
    {
        Code = "PHP",
        Name = "Philippine Peso",
        NumericCode = "608",
        Symbol = "P",
        MinorUnits = 2
    };

    private static UpdateCurrencyCommand NewUpdate() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Philippine Peso",
        NumericCode = "608",
        Symbol = "P",
        MinorUnits = 2
    };

    [Fact]
    public void AValidCurrencyIsAccepted()
    {
        Assert.True(Create.Validate(NewCreate()).IsValid);
        Assert.True(Update.Validate(NewUpdate()).IsValid);
    }

    [Fact]
    public void ABlankCodeIsRejectedRatherThanAllocated()
    {
        // The difference between this list and every other one in the module.
        var command = NewCreate();
        command.Code = "";

        Assert.False(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("php")]
    [InlineData(" PHP ")]
    public void ACodeIsCheckedOnItsNormalisedForm(string code)
    {
        var command = NewCreate();
        command.Code = code;

        Assert.True(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("PH")]
    [InlineData("PHPX")]
    [InlineData("60 8")]
    public void ACodeThatIsNotThreeLettersIsRejected(string code)
    {
        var command = NewCreate();
        command.Code = code;

        Assert.False(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("60")]
    [InlineData("6088")]
    public void ANumericCodeThatIsNotThreeDigitsIsRejected(string numeric)
    {
        var command = NewCreate();
        command.NumericCode = numeric;

        Assert.False(Create.Validate(command).IsValid);

        var update = NewUpdate();
        update.NumericCode = numeric;

        Assert.False(Update.Validate(update).IsValid);
    }

    [Fact]
    public void AnAbsentNumericCodeIsFine()
    {
        // A steward adding a currency by its familiar letters should not be blocked on looking the
        // number up.
        var command = NewCreate();
        command.NumericCode = null;

        Assert.True(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheMinorUnitsThatExistAreAccepted(int units)
    {
        var command = NewCreate();
        command.MinorUnits = units;

        Assert.True(Create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void MinorUnitsOutsideThatRangeAreRejected(int units)
    {
        // A typo here changes how every total in the currency rounds, and nothing else would catch
        // it - the number is only ever read, never validated against a payment.
        var command = NewCreate();
        command.MinorUnits = units;

        Assert.False(Create.Validate(command).IsValid);

        var update = NewUpdate();
        update.MinorUnits = units;

        Assert.False(Update.Validate(update).IsValid);
    }

    [Fact]
    public void OverridingTheDuplicateProbeNeedsAReason()
    {
        var command = NewCreate();
        command.ConfirmedDistinct = true;

        Assert.False(Create.Validate(command).IsValid);

        command.DistinctReason = "The Zimbabwean dollar, which is not the US one.";

        Assert.True(Create.Validate(command).IsValid);
    }
}
