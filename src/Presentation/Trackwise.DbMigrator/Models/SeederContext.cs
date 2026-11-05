using Trackwise.DbMigrator.Regions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.AspNetCore.Identity;

namespace Trackwise.DbMigrator.Models;

/// <summary>
/// Shared context passed to all seeders, containing services and shared state.
/// </summary>
public class SeederContext
{
    public required IServiceProvider ServiceProvider { get; init; }
    public required IConfiguration Configuration { get; init; }
    public required IMasterUnitOfWork MasterUnitOfWork { get; init; }
    public required ITenantUnitOfWork TenantUnitOfWork { get; set; }
    public required UserManager<User> UserManager { get; init; }
    public required RoleManager<AppRole> RoleManager { get; init; }

    /// <summary>Running against a Production database: demo placeholders and demo-only tenants are skipped.</summary>
    public bool IsProduction { get; init; }

    /// <summary>
    /// The tenant currently being seeded by the per-tenant orchestration loop.
    /// Set to null during the master-only Infrastructure pass.
    /// </summary>
    public Tenant? CurrentTenant
    {
        get => SharedState.TryGetValue(nameof(CurrentTenant), out var val) ? (Tenant)val : null;
        set => SharedState[nameof(CurrentTenant)] = value!;
    }

    /// <summary>
    /// Region profile bound to <see cref="CurrentTenant"/>. Drives all region-specific
    /// fake-data choices (route points, truck makes, plate formats, etc.).
    /// </summary>
    public IRegionProfile? Region
    {
        get => SharedState.TryGetValue(nameof(Region), out var val) ? (IRegionProfile)val : null;
        set => SharedState[nameof(Region)] = value!;
    }

    /// <summary><c>SeedData/*.json</c> section this tenant reads from. Null during the master-only Infrastructure pass.</summary>
    public string? SeedDataKey { get; set; }

    public OperatingMode OperatingMode { get; set; } = OperatingMode.Fleet;

    public double DataScale { get; set; } = 1.0;

    /// <summary>
    /// The user array from this tenant's seed-data section (e.g. <c>Us:Users</c> or <c>Solo:Users</c>),
    /// loaded from <c>SeedData/*.json</c>. Empty when the tenant has no seed set.
    /// </summary>
    public UserData[] GetSeedUsers()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Users").Get<UserData[]>() ?? [];
    }

    /// <summary>The real staff roster (<c>Ph:Staff</c>) from this tenant's seed-data section; empty when none.</summary>
    public StaffData[] GetSeedStaff()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Staff").Get<StaffData[]>() ?? [];
    }

    /// <summary>The real driver/helper roster (<c>Ph:Crew</c>) from this tenant's seed-data section; empty when none.</summary>
    public CrewData[] GetSeedCrew()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Crew").Get<CrewData[]>() ?? [];
    }

    /// <summary>The real counterparties (<c>Ph:BusinessPartners</c>) from this tenant's seed-data section; empty when none.</summary>
    public BusinessPartnerData[] GetSeedBusinessPartners()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:BusinessPartners").Get<BusinessPartnerData[]>() ?? [];
    }

    /// <summary>The real truck roster (<c>Ph:Fleet</c>) from this tenant's seed-data section; empty when none.</summary>
    public FleetTruckData[] GetSeedFleet()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Fleet").Get<FleetTruckData[]>() ?? [];
    }

    /// <summary>The real trailers and chassis (<c>Ph:Trailers</c>) from this tenant's seed-data section; empty when none.</summary>
    public FleetTrailerData[] GetSeedTrailers()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Trailers").Get<FleetTrailerData[]>() ?? [];
    }

    /// <summary>The real client delivery outlets (<c>Ph:Outlets</c>) from this tenant's seed-data section; empty when none.</summary>
    public OutletData[] GetSeedOutlets()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Outlets").Get<OutletData[]>() ?? [];
    }

    /// <summary>The clients' rate card revisions (<c>Ph:RateAgreements</c>) from this tenant's seed-data section; empty when none.</summary>
    public RateAgreementData[] GetSeedRateAgreements()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:RateAgreements").Get<RateAgreementData[]>() ?? [];
    }

    /// <summary>The trucks' repair history (<c>Ph:Repairs</c>) from this tenant's seed-data section; empty when none.</summary>
    public RepairData[] GetSeedRepairs()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Repairs").Get<RepairData[]>() ?? [];
    }

    /// <summary>The weekly report's cases (<c>Ph:Incidents</c>) from this tenant's seed-data section; empty when none.</summary>
    public IncidentData[] GetSeedIncidents()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:Incidents").Get<IncidentData[]>() ?? [];
    }

    /// <summary>The pattern of this tenant's case numbers (<c>Ph:IncidentSeries</c>); null when none.</summary>
    public IncidentSeriesData? GetSeedIncidentSeries()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? null
            : Configuration.GetSection($"{SeedDataKey}:IncidentSeries").Get<IncidentSeriesData>();
    }

    /// <summary>The reefers' weekly PMS (<c>Ph:ReeferPms</c>) from this tenant's seed-data section; empty when none.</summary>
    public ReeferPmsData[] GetSeedReeferPms()
    {
        return string.IsNullOrEmpty(SeedDataKey)
            ? []
            : Configuration.GetSection($"{SeedDataKey}:ReeferPms").Get<ReeferPmsData[]>() ?? [];
    }

    /// <summary>Scales a fleet-sized seed count for the current tenant. Never returns less than 1.</summary>
    public int Scale(int baseCount) => Math.Max(1, (int)Math.Floor(baseCount * DataScale));

    /// <summary>
    /// Shared state dictionary for passing data between seeders.
    /// </summary>
    private Dictionary<string, object> SharedState { get; } = [];

    public IList<User>? CreatedUsers
    {
        get => SharedState.TryGetValue(nameof(CreatedUsers), out var val) ? (IList<User>)val : null;
        set => SharedState[nameof(CreatedUsers)] = value!;
    }

    public CompanyEmployees? CreatedEmployees
    {
        get => SharedState.TryGetValue(nameof(CreatedEmployees), out var val) ? (CompanyEmployees)val : null;
        set => SharedState[nameof(CreatedEmployees)] = value!;
    }

    public IList<Customer>? CreatedCustomers
    {
        get => SharedState.TryGetValue(nameof(CreatedCustomers), out var val) ? (IList<Customer>)val : null;
        set => SharedState[nameof(CreatedCustomers)] = value!;
    }

    public IList<Truck>? CreatedTrucks
    {
        get => SharedState.TryGetValue(nameof(CreatedTrucks), out var val) ? (IList<Truck>)val : null;
        set => SharedState[nameof(CreatedTrucks)] = value!;
    }

    public IList<Terminal>? CreatedTerminals
    {
        get => SharedState.TryGetValue(nameof(CreatedTerminals), out var val) ? (IList<Terminal>)val : null;
        set => SharedState[nameof(CreatedTerminals)] = value!;
    }

    public IList<Container>? CreatedContainers
    {
        get => SharedState.TryGetValue(nameof(CreatedContainers), out var val) ? (IList<Container>)val : null;
        set => SharedState[nameof(CreatedContainers)] = value!;
    }

    public IList<CustomerUser>? CreatedCustomerUsers
    {
        get => SharedState.TryGetValue(nameof(CreatedCustomerUsers), out var val) ? (IList<CustomerUser>)val : null;
        set => SharedState[nameof(CreatedCustomerUsers)] = value!;
    }

    public IList<Branch>? CreatedBranches
    {
        get => SharedState.TryGetValue(nameof(CreatedBranches), out var val) ? (IList<Branch>)val : null;
        set => SharedState[nameof(CreatedBranches)] = value!;
    }

    public IList<Vessel>? CreatedVessels
    {
        get => SharedState.TryGetValue(nameof(CreatedVessels), out var val) ? (IList<Vessel>)val : null;
        set => SharedState[nameof(CreatedVessels)] = value!;
    }

    public IList<Commodity>? CreatedCommodities
    {
        get => SharedState.TryGetValue(nameof(CreatedCommodities), out var val) ? (IList<Commodity>)val : null;
        set => SharedState[nameof(CreatedCommodities)] = value!;
    }

    public IList<Location>? CreatedLocations
    {
        get => SharedState.TryGetValue(nameof(CreatedLocations), out var val) ? (IList<Location>)val : null;
        set => SharedState[nameof(CreatedLocations)] = value!;
    }

    public IList<Trailer>? CreatedTrailers
    {
        get => SharedState.TryGetValue(nameof(CreatedTrailers), out var val) ? (IList<Trailer>)val : null;
        set => SharedState[nameof(CreatedTrailers)] = value!;
    }

    public IList<Helper>? CreatedHelpers
    {
        get => SharedState.TryGetValue(nameof(CreatedHelpers), out var val) ? (IList<Helper>)val : null;
        set => SharedState[nameof(CreatedHelpers)] = value!;
    }

    public IList<ChargeCode>? CreatedChargeCodes
    {
        get => SharedState.TryGetValue(nameof(CreatedChargeCodes), out var val) ? (IList<ChargeCode>)val : null;
        set => SharedState[nameof(CreatedChargeCodes)] = value!;
    }

    public IList<TariffRate>? CreatedTariffRates
    {
        get => SharedState.TryGetValue(nameof(CreatedTariffRates), out var val) ? (IList<TariffRate>)val : null;
        set => SharedState[nameof(CreatedTariffRates)] = value!;
    }

    public IList<BusinessPartner>? CreatedBusinessPartners
    {
        get => SharedState.TryGetValue(nameof(CreatedBusinessPartners), out var val) ? (IList<BusinessPartner>)val : null;
        set => SharedState[nameof(CreatedBusinessPartners)] = value!;
    }

    public IList<Quotation>? CreatedQuotations
    {
        get => SharedState.TryGetValue(nameof(CreatedQuotations), out var val) ? (IList<Quotation>)val : null;
        set => SharedState[nameof(CreatedQuotations)] = value!;
    }

    public IList<Booking>? CreatedBookings
    {
        get => SharedState.TryGetValue(nameof(CreatedBookings), out var val) ? (IList<Booking>)val : null;
        set => SharedState[nameof(CreatedBookings)] = value!;
    }

    /// <summary>
    /// Resets per-tenant shared state between tenant iterations.
    /// </summary>
    public void ResetPerTenantState()
    {
        SharedState.Remove(nameof(CreatedUsers));
        SharedState.Remove(nameof(CreatedEmployees));
        SharedState.Remove(nameof(CreatedCustomers));
        SharedState.Remove(nameof(CreatedTrucks));
        SharedState.Remove(nameof(CreatedTerminals));
        SharedState.Remove(nameof(CreatedContainers));
        SharedState.Remove(nameof(CreatedCustomerUsers));
        SharedState.Remove(nameof(CreatedBranches));
        SharedState.Remove(nameof(CreatedVessels));
        SharedState.Remove(nameof(CreatedCommodities));
        SharedState.Remove(nameof(CreatedLocations));
        SharedState.Remove(nameof(CreatedTrailers));
        SharedState.Remove(nameof(CreatedHelpers));
        SharedState.Remove(nameof(CreatedChargeCodes));
        SharedState.Remove(nameof(CreatedTariffRates));
        SharedState.Remove(nameof(CreatedBusinessPartners));
        SharedState.Remove(nameof(CreatedQuotations));
        SharedState.Remove(nameof(CreatedBookings));
    }
}
