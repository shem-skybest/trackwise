using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Regions;
using Trackwise.DbMigrator.Seeders.FakeData;
using Trackwise.DbMigrator.Seeders.Infrastructure;

namespace Trackwise.DbMigrator.Extensions;

/// <summary>
///     Extension methods for registering seeders with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers all seeders.
    /// </summary>
    public static IServiceCollection AddSeeders(this IServiceCollection services)
    {
        // Region profiles + factory
        services.AddSingleton<IRegionProfile, USRegionProfile>();
        services.AddSingleton<IRegionProfile, EURegionProfile>();
        services.AddSingleton<IRegionProfile, PHRegionProfile>();
        services.AddSingleton<IRegionProfileFactory, RegionProfileFactory>();

        // Infrastructure seeders (master DB unless IsTenantScoped is overridden)
        services.AddScoped<ISeeder, AppRoleSeeder>();
        services.AddScoped<ISeeder, SuperAdminSeeder>();
        services.AddScoped<ISeeder, SubscriptionPlanSeeder>();
        services.AddScoped<ISeeder, StripeSeeder>();
        services.AddScoped<ISeeder, StripeSubscriptionSyncSeeder>();
        services.AddScoped<ISeeder, DemoTenantsSeeder>();
        services.AddScoped<ISeeder, TenantRoleSeeder>();
        services.AddScoped<ISeeder, TenantTaxRateSeeder>();
        services.AddScoped<ISeeder, IftaTaxRateSeeder>();

        // FakeData seeders (always tenant-scoped - run once per demo tenant)
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, EmployeeSeeder>();
        services.AddScoped<ISeeder, StaffSeeder>();
        services.AddScoped<ISeeder, CrewSeeder>();
        services.AddScoped<ISeeder, CustomerSeeder>();
        services.AddScoped<ISeeder, TruckSeeder>();
        services.AddScoped<ISeeder, MaintenanceSeeder>();
        services.AddScoped<ISeeder, TerminalSeeder>();
        services.AddScoped<ISeeder, ContainerSeeder>();
        services.AddScoped<ISeeder, LoadSeeder>();
        services.AddScoped<ISeeder, DocumentSeeder>();
        services.AddScoped<ISeeder, ConditionReportSeeder>();
        services.AddScoped<ISeeder, DvirReportSeeder>();
        services.AddScoped<ISeeder, DriverBehaviorEventSeeder>();
        services.AddScoped<ISeeder, DriverHosStatusSeeder>();
        services.AddScoped<ISeeder, IncidentSeeder>();
        services.AddScoped<ISeeder, ExpenseSeeder>();
        services.AddScoped<ISeeder, FuelCardSeeder>();
        services.AddScoped<ISeeder, IftaMileageSeeder>();
        services.AddScoped<ISeeder, TripSeeder>();
        services.AddScoped<ISeeder, NegotiationSeeder>();
        services.AddScoped<ISeeder, InvoiceVatSeeder>();
        services.AddScoped<ISeeder, NotificationSeeder>();
        services.AddScoped<ISeeder, PayrollSeeder>();
        services.AddScoped<ISeeder, CustomerUserSeeder>();

        // Master Data / Freight Forwarding fake-data seeders
        services.AddScoped<ISeeder, BranchSeeder>();
        services.AddScoped<ISeeder, VesselSeeder>();
        services.AddScoped<ISeeder, CommoditySeeder>();
        services.AddScoped<ISeeder, LocationSeeder>();
        services.AddScoped<ISeeder, TrailerSeeder>();
        services.AddScoped<ISeeder, HelperSeeder>();
        services.AddScoped<ISeeder, FleetSeeder>();
        services.AddScoped<ISeeder, VehiclePermitSeeder>();
        services.AddScoped<ISeeder, RepairHistorySeeder>();
        services.AddScoped<ISeeder, MondayIncidentSeeder>();
        services.AddScoped<ISeeder, ReeferPmsSeeder>();
        services.AddScoped<ISeeder, OutletSeeder>();
        services.AddScoped<ISeeder, RateAgreementSeeder>();
        services.AddScoped<ISeeder, GensetSeeder>();
        services.AddScoped<ISeeder, ChargeCodeSeeder>();
        services.AddScoped<ISeeder, TariffSeeder>();
        services.AddScoped<ISeeder, BusinessPartnerSeeder>();
        services.AddScoped<ISeeder, QuotationSeeder>();
        services.AddScoped<ISeeder, BookingSeeder>();
        services.AddScoped<ISeeder, WorkfileSeeder>();

        // note seeder
        services.AddScoped<ISeeder, NoteSeeder>();

        return services;
    }
}
