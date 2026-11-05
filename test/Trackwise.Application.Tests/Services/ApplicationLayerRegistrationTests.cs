using Trackwise.Application.Modules.Compliance.Privacy.Services;
using Trackwise.Application.Modules.Compliance.Safety.Services;
using Trackwise.Application.Modules.Financial.Payroll.Services;
using Trackwise.Application.Modules.Financial.Tax.Services;
using Trackwise.Application.Modules.IdentityAccess.Invitations.Services;
using Trackwise.Application.Modules.IdentityAccess.Users.Services;
using Trackwise.Application.Modules.Operations.Loads.Services;
using Trackwise.Application.Abstractions.Dispatch;
using Trackwise.Application.Abstractions.Realtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Trackwise.Application.Tests.Services;

public class ApplicationLayerRegistrationTests
{
    [Theory]
    [InlineData(typeof(ILoadService))]
    [InlineData(typeof(IPayrollService))]
    [InlineData(typeof(IUserService))]
    [InlineData(typeof(IMaintenanceReminderService))]
    [InlineData(typeof(ILicenseExpiryReminderService))]
    [InlineData(typeof(IInvitationExpiryService))]
    [InlineData(typeof(IDispatchEligibilityService))]
    [InlineData(typeof(ITruckGeolocationUpdater))]
    [InlineData(typeof(IDataExportProcessingService))]
    [InlineData(typeof(IDataDeletionProcessingService))]
    [InlineData(typeof(IDataRetentionService))]
    [InlineData(typeof(IDataExportExpiryService))]
    [InlineData(typeof(IInvoiceTaxApplier))]
    public void AddApplicationLayer_RegistersService(Type serviceType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationLayer();

        var descriptor = services.LastOrDefault(d => d.ServiceType == serviceType);

        Assert.NotNull(descriptor);
    }

    [Theory]
    [InlineData(typeof(ILoadService))]
    [InlineData(typeof(IPayrollService))]
    [InlineData(typeof(IUserService))]
    [InlineData(typeof(IInvoiceTaxApplier))]
    public void AddApplicationLayer_RegistersAsScoped(Type serviceType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationLayer();

        var descriptor = services.LastOrDefault(d => d.ServiceType == serviceType);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }
}
