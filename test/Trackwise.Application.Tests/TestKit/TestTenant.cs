using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Tests.TestKit;

internal static class TestTenant
{
    public static Tenant Create(string? companyName = null, string? mcNumber = null) => new()
    {
        Name = "test",
        CompanyName = companyName,
        McNumber = mcNumber,
        ConnectionString = "test",
        BillingEmail = "billing@test.com",
        CompanyAddress = new Address
        {
            StreetAddress = "1 Test St",
            CityOrMunicipality = "Test",
            Province = "TX",
            Region = "",
            Zipcode = "00000",
            Country = "US"
        }
    };
}
