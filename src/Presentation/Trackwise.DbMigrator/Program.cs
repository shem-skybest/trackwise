using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Trackwise.Application.Modules.Financial.Payroll.Services;
using Trackwise.DbMigrator.Data;
using Trackwise.Infrastructure.Persistence.Data;
using Trackwise.DbMigrator.Extensions;
using Trackwise.DbMigrator.Services;
using Trackwise.DbMigrator.Workers;
using Trackwise.Application;
using Trackwise.Infrastructure.Integrations.FuelCards;
using Trackwise.Infrastructure.Payments;
using Trackwise.Infrastructure.Persistence;
using Trackwise.Infrastructure.Tax;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("SeedData/ph.json", optional: true);
builder.Configuration.AddJsonFile("SeedData/ph-outlets.json", optional: true);
builder.Configuration.AddJsonFile("SeedData/ph-rate-agreements.json", optional: true);
builder.Configuration.AddJsonFile("SeedData/ph-fleet.json", optional: true);
builder.Configuration.AddJsonFile("SeedData/ph-repairs.json", optional: true);
builder.Configuration.AddJsonFile("SeedData/ph-incidents.json", optional: true);
builder.Configuration.AddJsonFile("SeedData/eu.json", optional: true);
builder.Configuration.AddJsonFile("SeedData/solo.json", optional: true);
builder.Configuration["Tax:Provider"] ??= "manual";

builder.Services.AddPersistenceInfrastructure(builder.Configuration)
    .AddMasterDatabase()
    .AddTenantDatabase()
    .AddIdentity();

// Duende operational store (signing keys + grants) in the master DB, migrated before IdentityServer needs it
builder.Services.AddSingleton(DuendeOperationalStore.ConfigureStoreOptions(new OperationalStoreOptions()));
builder.Services.AddDbContext<PersistedGrantDbContext>(options =>
    DuendeOperationalStore.ConfigureDbContext(
        options, builder.Configuration.GetConnectionString("MasterDatabase")));

builder.Services.AddApplicationTaxServices();
builder.Services.AddApplicationFuelCardServices();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddTaxInfrastructure(builder.Configuration);
builder.Services.AddFuelCardIntegrations(builder.Configuration);
builder.Services.AddScoped<PayrollService>();
builder.Services.AddSeeders();

// These hosted services will run in the order they are registered
builder.Services.AddHostedService<MigrateDatabaseWorker>();
builder.Services.AddHostedService<SeederOrchestrationWorker>();
builder.Services.AddHostedService<CreateSqlFunctionsWorker>();

// `--exit` stops the host once the workers above have completed, for one-shot runs
if (args.Contains("--exit"))
{
    builder.Services.AddHostedService<StopApplicationWorker>();
}

builder.Build().Run();
