using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Persistence.Converters;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.Converters;

public class SnakeCaseEnumConverterTests
{
    #region Test enums

    public enum SimpleEnum { Draft, Active, Completed }
    public enum TwoWordEnum { InTransit, PickedUp, OnHold }
    public enum AcronymEnum { USD, US, API }
    public enum MixedAcronymEnum { ApiAccess, USDate, ELDProvider, HoSLog }
    public enum SingleValueEnum { Default }
    public enum NumberSuffixEnum { Gpt54, Version2 }

    // Member names that already contain underscores (e.g. EquipmentType.REEFER_20) don't
    // round-trip through the naive PascalCase reconstruction the cache-miss fallback uses -
    // "reefer_20" -> ToPascalCase -> "Reefer20", which doesn't match "REEFER_20". The cache
    // itself has the correct mapping; the bug was that the fallback ran unconditionally
    // (Dictionary.GetValueOrDefault eagerly evaluates its default-value argument) instead of
    // only on an actual cache miss.
    public enum PreUnderscoredEnum { DRY_VAN_20, REEFER_20, DRY_VAN_40_HC }

    [Flags]
    public enum FlagsEnum { None = 0, A = 1, B = 2, C = 4 }

    #endregion

    #region Enum to snake_case (serialization)

    [Theory]
    [InlineData(SimpleEnum.Draft, "draft")]
    [InlineData(SimpleEnum.Active, "active")]
    [InlineData(SimpleEnum.Completed, "completed")]
    public void ToSnakeCase_SimpleWords(SimpleEnum value, string expected)
    {
        var converter = new SnakeCaseEnumConverter<SimpleEnum>();
        var result = ToProvider(converter, value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(TwoWordEnum.InTransit, "in_transit")]
    [InlineData(TwoWordEnum.PickedUp, "picked_up")]
    [InlineData(TwoWordEnum.OnHold, "on_hold")]
    public void ToSnakeCase_MultiWordValues(TwoWordEnum value, string expected)
    {
        var converter = new SnakeCaseEnumConverter<TwoWordEnum>();
        var result = ToProvider(converter, value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AcronymEnum.USD, "usd")]
    [InlineData(AcronymEnum.US, "us")]
    [InlineData(AcronymEnum.API, "api")]
    public void ToSnakeCase_Acronyms_NotSplit(AcronymEnum value, string expected)
    {
        var converter = new SnakeCaseEnumConverter<AcronymEnum>();
        var result = ToProvider(converter, value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(MixedAcronymEnum.ApiAccess, "api_access")]
    [InlineData(MixedAcronymEnum.USDate, "us_date")]
    [InlineData(MixedAcronymEnum.ELDProvider, "eld_provider")]
    [InlineData(MixedAcronymEnum.HoSLog, "ho_s_log")]
    public void ToSnakeCase_MixedAcronyms(MixedAcronymEnum value, string expected)
    {
        var converter = new SnakeCaseEnumConverter<MixedAcronymEnum>();
        var result = ToProvider(converter, value);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToSnakeCase_SingleValue()
    {
        var converter = new SnakeCaseEnumConverter<SingleValueEnum>();
        var result = ToProvider(converter, SingleValueEnum.Default);
        Assert.Equal("default", result);
    }

    [Theory]
    [InlineData(NumberSuffixEnum.Gpt54, "gpt54")]
    [InlineData(NumberSuffixEnum.Version2, "version2")]
    public void ToSnakeCase_NumberSuffix(NumberSuffixEnum value, string expected)
    {
        var converter = new SnakeCaseEnumConverter<NumberSuffixEnum>();
        var result = ToProvider(converter, value);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Snake_case to enum (deserialization)

    [Theory]
    [InlineData("draft", SimpleEnum.Draft)]
    [InlineData("active", SimpleEnum.Active)]
    [InlineData("completed", SimpleEnum.Completed)]
    public void FromSnakeCase_SimpleWords(string input, SimpleEnum expected)
    {
        var converter = new SnakeCaseEnumConverter<SimpleEnum>();
        var result = FromProvider(converter, input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("in_transit", TwoWordEnum.InTransit)]
    [InlineData("picked_up", TwoWordEnum.PickedUp)]
    [InlineData("on_hold", TwoWordEnum.OnHold)]
    public void FromSnakeCase_MultiWordValues(string input, TwoWordEnum expected)
    {
        var converter = new SnakeCaseEnumConverter<TwoWordEnum>();
        var result = FromProvider(converter, input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("usd", AcronymEnum.USD)]
    [InlineData("us", AcronymEnum.US)]
    [InlineData("api", AcronymEnum.API)]
    public void FromSnakeCase_Acronyms(string input, AcronymEnum expected)
    {
        var converter = new SnakeCaseEnumConverter<AcronymEnum>();
        var result = FromProvider(converter, input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("api_access", MixedAcronymEnum.ApiAccess)]
    [InlineData("eld_provider", MixedAcronymEnum.ELDProvider)]
    public void FromSnakeCase_MixedAcronyms(string input, MixedAcronymEnum expected)
    {
        var converter = new SnakeCaseEnumConverter<MixedAcronymEnum>();
        var result = FromProvider(converter, input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromSnakeCase_EmptyString_ReturnsDefault()
    {
        // Existing rows can carry a "" default left over from an AddColumn migration
        // (e.g. trucks.adr_equipment_allowed_classes). Must not throw.
        var converter = new SnakeCaseEnumConverter<FlagsEnum>();
        var result = FromProvider(converter, "");
        Assert.Equal(FlagsEnum.None, result);
    }

    [Fact]
    public void FromSnakeCase_NullString_ReturnsDefault()
    {
        var converter = new SnakeCaseEnumConverter<FlagsEnum>();
        var result = FromProvider(converter, null!);
        Assert.Equal(FlagsEnum.None, result);
    }

    #endregion

    #region Roundtrip

    // A Philippine license lists several DL codes, so the combined value (not in the member cache)
    // goes through the fallback path - including digit and two-capital members like B1 and CE.
    [Theory]
    [InlineData(PhilippineDlCode.None)]
    [InlineData(PhilippineDlCode.C)]
    [InlineData(PhilippineDlCode.B | PhilippineDlCode.B1 | PhilippineDlCode.B2 | PhilippineDlCode.C)]
    [InlineData(PhilippineDlCode.A1 | PhilippineDlCode.BE | PhilippineDlCode.CE)]
    public void Roundtrip_PhilippineDlCodeCombinations(PhilippineDlCode value)
    {
        var converter = new SnakeCaseEnumConverter<PhilippineDlCode>();
        var serialized = ToProvider(converter, value);
        var deserialized = FromProvider(converter, serialized);
        Assert.Equal(value, deserialized);
    }

    [Theory]
    [InlineData(SimpleEnum.Draft)]
    [InlineData(SimpleEnum.Active)]
    [InlineData(SimpleEnum.Completed)]
    public void Roundtrip_SimpleEnum(SimpleEnum value)
    {
        var converter = new SnakeCaseEnumConverter<SimpleEnum>();
        var serialized = ToProvider(converter, value);
        var deserialized = FromProvider(converter, serialized);
        Assert.Equal(value, deserialized);
    }

    [Theory]
    [InlineData(TwoWordEnum.InTransit)]
    [InlineData(TwoWordEnum.PickedUp)]
    [InlineData(TwoWordEnum.OnHold)]
    public void Roundtrip_MultiWordEnum(TwoWordEnum value)
    {
        var converter = new SnakeCaseEnumConverter<TwoWordEnum>();
        var serialized = ToProvider(converter, value);
        var deserialized = FromProvider(converter, serialized);
        Assert.Equal(value, deserialized);
    }

    [Theory]
    [InlineData(AcronymEnum.USD)]
    [InlineData(AcronymEnum.US)]
    [InlineData(AcronymEnum.API)]
    public void Roundtrip_AcronymEnum(AcronymEnum value)
    {
        var converter = new SnakeCaseEnumConverter<AcronymEnum>();
        var serialized = ToProvider(converter, value);
        var deserialized = FromProvider(converter, serialized);
        Assert.Equal(value, deserialized);
    }

    [Theory]
    [InlineData(PreUnderscoredEnum.DRY_VAN_20, "dry_van_20")]
    [InlineData(PreUnderscoredEnum.REEFER_20, "reefer_20")]
    [InlineData(PreUnderscoredEnum.DRY_VAN_40_HC, "dry_van_40_hc")]
    public void FromSnakeCase_MemberNameAlreadyContainsUnderscores_UsesCachedValue(
        PreUnderscoredEnum expected, string input)
    {
        // Regression test: FromSnake used to unconditionally evaluate the Enum.Parse fallback
        // (via Dictionary.GetValueOrDefault's eager argument), which throws for values like this
        // before the correct cached lookup is ever consulted.
        var converter = new SnakeCaseEnumConverter<PreUnderscoredEnum>();
        var result = FromProvider(converter, input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PreUnderscoredEnum.DRY_VAN_20)]
    [InlineData(PreUnderscoredEnum.REEFER_20)]
    [InlineData(PreUnderscoredEnum.DRY_VAN_40_HC)]
    public void Roundtrip_MemberNameAlreadyContainsUnderscores(PreUnderscoredEnum value)
    {
        var converter = new SnakeCaseEnumConverter<PreUnderscoredEnum>();
        var serialized = ToProvider(converter, value);
        var deserialized = FromProvider(converter, serialized);
        Assert.Equal(value, deserialized);
    }

    #endregion

    #region Helpers

    private static string ToProvider<T>(SnakeCaseEnumConverter<T> converter, T value)
        where T : struct, Enum
    {
        // Access the converter's ConvertToProviderExpression and compile it
        var func = converter.ConvertToProviderExpression.Compile();
        return func(value);
    }

    private static T FromProvider<T>(SnakeCaseEnumConverter<T> converter, string value)
        where T : struct, Enum
    {
        var func = converter.ConvertFromProviderExpression.Compile();
        return func(value);
    }

    #endregion
}
