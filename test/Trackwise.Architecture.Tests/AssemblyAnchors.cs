// Force every project under test to be loaded into the AppDomain and exposes
// the ArchUnitNET Architecture used across all boundary/handler tests.
// Each anchor references a real public type from the target assembly's Registrar
// (touching them is enough to pull the assembly in).
//
// Adding a new infrastructure project? Add its anchor to AllInfrastructure below - the
// ArchLoader architecture and the boundary Theory both derive from that one array.

using ArchUnitNET.Loader;
using Trackwise.Application.Abstractions.CurrentUser;
using Assembly = System.Reflection.Assembly;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Trackwise.Architecture.Tests;

internal static class AssemblyAnchors
{
    public static readonly Assembly Application = typeof(Trackwise.Application.Registrar).Assembly;
    public static readonly Assembly ApplicationAbstractions = typeof(ICurrentUserService).Assembly;
    public static readonly Assembly Domain = typeof(Trackwise.Domain.Entities.Tenant).Assembly;

    public static readonly Assembly InfrastructureAI = typeof(Trackwise.Infrastructure.AI.Registrar).Assembly;
    public static readonly Assembly InfrastructureAccounting = typeof(Trackwise.Infrastructure.Integrations.Accounting.Registrar).Assembly;
    public static readonly Assembly InfrastructureIntegrationsCommon = typeof(Trackwise.Infrastructure.Integrations.Common.HttpClientJsonExtensions).Assembly;
    public static readonly Assembly InfrastructureCommunications = typeof(Trackwise.Infrastructure.Communications.Registrar).Assembly;
    public static readonly Assembly InfrastructureDocuments = typeof(Trackwise.Infrastructure.Documents.Registrar).Assembly;
    public static readonly Assembly InfrastructureEld = typeof(Trackwise.Infrastructure.Integrations.Eld.Registrar).Assembly;
    public static readonly Assembly InfrastructureFuelCards = typeof(Trackwise.Infrastructure.Integrations.FuelCards.Registrar).Assembly;
    public static readonly Assembly InfrastructureLoadBoard = typeof(Trackwise.Infrastructure.Integrations.LoadBoard.Registrar).Assembly;
    public static readonly Assembly InfrastructurePayments = typeof(Trackwise.Infrastructure.Payments.Registrar).Assembly;
    public static readonly Assembly InfrastructurePersistence = typeof(Trackwise.Infrastructure.Persistence.Registrar).Assembly;
    public static readonly Assembly InfrastructureRouting = typeof(Trackwise.Infrastructure.Routing.Registrar).Assembly;
    public static readonly Assembly InfrastructureStorage = typeof(Trackwise.Infrastructure.Storage.Registrar).Assembly;
    public static readonly Assembly InfrastructureTax = typeof(Trackwise.Infrastructure.Tax.Registrar).Assembly;
    public static readonly Assembly InfrastructureVin = typeof(Trackwise.Infrastructure.Vin.Registrar).Assembly;

    public static readonly Assembly[] AllInfrastructure =
    [
        InfrastructureAI,
        InfrastructureAccounting,
        InfrastructureIntegrationsCommon,
        InfrastructureCommunications,
        InfrastructureDocuments,
        InfrastructureEld,
        InfrastructureFuelCards,
        InfrastructureLoadBoard,
        InfrastructurePayments,
        InfrastructurePersistence,
        InfrastructureRouting,
        InfrastructureStorage,
        InfrastructureTax,
        InfrastructureVin,
    ];

    /// <summary>
    /// Infrastructure assemblies exempt from the "must not depend on Application" boundary rule.
    /// Trackwise.Infrastructure.AI stays here until slice 1.9-AI decouples it. See REFACTOR-INDEX.md.
    /// </summary>
    public static readonly string[] BoundaryExempt = ["Trackwise.Infrastructure.AI"];

    public static Assembly ByName(string name) =>
        AllInfrastructure.FirstOrDefault(a => a.GetName().Name == name)
        ?? throw new InvalidOperationException($"No infrastructure anchor for assembly '{name}'.");

    public static readonly ArchitectureModel Architecture = new ArchLoader()
        .LoadAssemblies([Application, ApplicationAbstractions, Domain, .. AllInfrastructure])
        .Build();
}
