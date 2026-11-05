using FluentValidation.TestHelper;
using Trackwise.Application.Validators;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Validators;

public class AddressValidatorTests
{
    private readonly AddressValidator sut = new();

    private static Address ValidAddress() => new()
    {
        StreetAddress = "1600 Amphitheatre Pkwy",
        CityOrMunicipality = "Mountain View",
        Province = "CA",
        Region = "California",
        Zipcode = "94043",
        Country = "US",
    };

    [Fact]
    public void Validate_FullyPopulated_Passes()
    {
        var result = sut.TestValidate(ValidAddress());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("US", "CA", "California", "94043")]
    [InlineData("CA", "ON", "Ontario", "M5V 3A8")]
    [InlineData("AU", "NSW", "New South Wales", "2000")]
    [InlineData("DE", "Bayern", "Bayern", "80331")]
    [InlineData("NL", "Noord-Holland", "Noord-Holland", "1012")]
    [InlineData("BE", "Antwerpen", "Antwerpen", "2000")]
    [InlineData("IE", "Dublin", "Dublin", "D02")]
    [InlineData("GB", "Greater London", "Greater London", "SW1A")]
    [InlineData("FR", "Île-de-France", "Île-de-France", "75001")]
    [InlineData("MX", "Jalisco", "Jalisco", "44100")]
    [InlineData("PH", "Metro Manila", "NCR", "1000")]
    public void Validate_ProvinceAndRegionProvided_Passes(string country, string province, string region, string zipcode)
    {
        var address = ValidAddress() with { Country = country, Province = province, Region = region, Zipcode = zipcode };
        var result = sut.TestValidate(address);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("US")]
    [InlineData("DE")]
    [InlineData("NL")]
    [InlineData("PH")]
    public void Validate_ProvinceMissing_FailsForAllCountries(string country)
    {
        var address = ValidAddress() with { Country = country, Province = "" };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.Province);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("DE")]
    [InlineData("PH")]
    public void Validate_RegionMissing_FailsForAllCountries(string country)
    {
        var address = ValidAddress() with { Country = country, Region = "" };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.Region);
    }

    [Fact]
    public void Validate_StreetAddressEmpty_Fails()
    {
        var address = ValidAddress() with { StreetAddress = "" };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.StreetAddress);
    }

    [Fact]
    public void Validate_CityOrMunicipalityEmpty_Fails()
    {
        var address = ValidAddress() with { CityOrMunicipality = "" };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.CityOrMunicipality);
    }

    [Fact]
    public void Validate_ZipcodeEmpty_Fails()
    {
        var address = ValidAddress() with { Zipcode = "" };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.Zipcode);
    }

    [Fact]
    public void Validate_PhilippineZipcodeEmpty_Passes()
    {
        var address = ValidAddress() with { Country = "PH", Zipcode = "" };
        var result = sut.TestValidate(address);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CountryEmpty_Fails()
    {
        var address = ValidAddress() with { Country = "" };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.Country);
    }

    [Fact]
    public void Validate_CountryOverMaxLength_Fails()
    {
        var address = ValidAddress() with { Country = new string('x', 101) };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.Country);
    }

    [Fact]
    public void Validate_CountryFullName_Passes()
    {
        var address = ValidAddress() with { Country = "Philippines" };
        var result = sut.TestValidate(address);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_StreetAddressOverMaxLength_Fails()
    {
        var address = ValidAddress() with { StreetAddress = new string('x', 201) };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.StreetAddress);
    }

    [Theory]
    [InlineData("1006")]
    [InlineData("6014")]
    public void Validate_PhilippineZipcodeValid_Passes(string zipcode)
    {
        var address = ValidAddress() with { Country = "PH", Zipcode = zipcode };
        var result = sut.TestValidate(address);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("100")]
    [InlineData("10000")]
    [InlineData("ABCD")]
    public void Validate_PhilippineZipcodeInvalid_Fails(string zipcode)
    {
        var address = ValidAddress() with { Country = "PH", Zipcode = zipcode };
        var result = sut.TestValidate(address);
        result.ShouldHaveValidationErrorFor(a => a.Zipcode)
            .WithErrorMessage("Philippine ZIP codes must be exactly 4 digits.");
    }
}
