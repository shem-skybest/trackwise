using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.Configurations;

/// <summary>
/// The workfile header's shipping-line / trucker / broker pickers filter partners by role, so the
/// role list has to be a real queryable column. SnakeCaseEnumConvention only reaches scalar enum
/// properties, so BusinessPartnerConfiguration applies the converter to the collection's element
/// type by hand - and a converter that silently stopped applying would store integers, matching
/// nothing and failing no other test. These force the SQL via <c>ToQueryString</c>.
/// </summary>
public class BusinessPartnerRoleQueryTranslationTests
{
    [Fact]
    public void RoleFilter_TranslatesToSqlRatherThanEvaluatingInMemory()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<BusinessPartner>()
            .Where(p => p.Roles.Contains(BusinessPartnerRole.ShippingLine))
            .ToQueryString();

        Assert.Contains("roles", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RolesAreStoredAsSnakeCaseText_NotEnumOrdinals()
    {
        using var db = new TenantDbContext();

        var property = db.Model.FindEntityType(typeof(BusinessPartner))!.FindProperty(nameof(BusinessPartner.Roles))!;
        var converter = property.GetElementType()!.GetValueConverter();

        Assert.NotNull(converter);
        Assert.Equal(typeof(string), converter.ProviderClrType);
        Assert.Equal("shipping_line", converter.ConvertToProvider(BusinessPartnerRole.ShippingLine));
        Assert.Equal("co_loader", converter.ConvertToProvider(BusinessPartnerRole.CoLoader));
    }
}
