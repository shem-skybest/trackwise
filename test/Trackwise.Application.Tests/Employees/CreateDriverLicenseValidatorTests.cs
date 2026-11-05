using Trackwise.Application.Modules.IdentityAccess.Employees.Commands;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Employees;

public class CreateDriverLicenseValidatorTests
{
    private static CreateDriverLicenseCommand Valid(Action<CreateDriverLicenseCommand>? tweak = null)
    {
        var command = new CreateDriverLicenseCommand
        {
            EmployeeId = Guid.NewGuid(),
            LicenseNumber = "N01-23-456789",
            LicenseClass = LicenseClass.PHProfessional,
            DlCodes = [PhilippineDlCode.B2, PhilippineDlCode.C],
            IssuingCountry = "PH",
            IssuedDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2034, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        tweak?.Invoke(command);
        return command;
    }

    private static IEnumerable<string> Errors(CreateDriverLicenseCommand command) =>
        new CreateDriverLicenseValidator().Validate(command).Errors.Select(e => e.ErrorMessage);

    [Fact]
    public void Validate_PhilippineProfessionalWithCodes_IsValid()
    {
        Assert.Empty(Errors(Valid()));
    }

    [Fact]
    public void Validate_PhilippineLicenseWithoutCodes_Fails()
    {
        Assert.Contains(Errors(Valid(c => c.DlCodes = [])), e => e.Contains("at least one DL code"));
    }

    [Theory]
    [InlineData(LicenseClass.PHProfessional, "US")]
    [InlineData(LicenseClass.USClassA, "PH")]
    [InlineData(LicenseClass.EUC, "PH")]
    public void Validate_ClassFromAnotherCountry_Fails(LicenseClass cls, string country)
    {
        var errors = Errors(Valid(c =>
        {
            c.LicenseClass = cls;
            c.IssuingCountry = country;
            c.DlCodes = cls.IsPhilippine() ? c.DlCodes : [];
        }));

        Assert.Contains(errors, e => e.Contains("not valid for the issuing country"));
    }

    [Fact]
    public void Validate_DlCodesOnAUsLicense_Fails()
    {
        var errors = Errors(Valid(c =>
        {
            c.LicenseClass = LicenseClass.USClassA;
            c.IssuingCountry = "US";
        }));

        Assert.Contains(errors, e => e.Contains("only apply to Philippine licenses"));
    }

    [Fact]
    public void Validate_EuLicenseFromGermany_StillValid()
    {
        var errors = Errors(Valid(c =>
        {
            c.LicenseClass = LicenseClass.EUCE;
            c.IssuingCountry = "DE";
            c.DlCodes = [];
        }));

        Assert.Empty(errors);
    }
}
