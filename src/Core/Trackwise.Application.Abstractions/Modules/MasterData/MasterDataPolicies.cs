using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// The governance table for every master-data entity: approval mode, how duplicates are found,
/// whether it is time-bound, whether it is branch-scoped.
/// <para>
/// This is the single place per-entity behaviour is declared. Adding an entity to the module means
/// adding an entry here - never a new branch in a handler or pipeline stage. If you find yourself
/// writing <c>if (entityType == "Customer")</c> anywhere downstream, the fact belongs here instead.
/// </para>
/// <para>
/// <b>Table and column names in this file are trusted.</b> The duplicate probe interpolates them
/// into SQL for trigram matching, so they must stay compile-time constants and must never be built
/// from request data.
/// </para>
/// </summary>
public static class MasterDataPolicies
{
    /// <summary>
    /// A branch is identified by its code alone - there are a handful of offices and stewards know
    /// them by name, so fuzzy matching would cost more false positives than it prevents duplicates.
    /// </summary>
    public static readonly MasterDataPolicy Branch = new()
    {
        EntityType = nameof(Domain.Entities.Branch),
        Group = MasterDataGroup.ReferenceList,
        TableName = "branches",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = false,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),
            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // Every table carrying a branch_id belongs here. Ports are the first; trucks, staff and
        // the rest of IBranchScoped join them as they land. A branch that still owns records is
        // exactly the retirement a steward should be stopped on - closing the office out from under
        // them would leave records scoped to something that no longer resolves.
        DependencyRules =
        [
            new DependencyRule("ports", "terminals", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("vessels", "vessels", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("commodities", "commodities", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("locations", "locations", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("business partners", "business_partners", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("trailers", "trailers", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("helpers", "helpers", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("trucks", "trucks", "branch_id", LiveCondition: "deleted_at IS NULL"),
            new DependencyRule("employees", "employees", "branch_id", LiveCondition: "deleted_at IS NULL")
        ],

        // Offices are few and deliberately named, so a near-identical name is worth stopping on.
        HardThreshold = 0.80,
        SoftThreshold = 0.45
    };

    /// <summary>
    /// A port, kept on the existing <see cref="Domain.Entities.Terminal"/> rather than a parallel
    /// record. The first entity with real dependents and the first genuine test of fuzzy matching:
    /// legacy port names arrive abbreviated, misspelled and suffixed with terminal numbers, which
    /// is the class of duplicate exact matching never catches.
    /// </summary>
    public static readonly MasterDataPolicy Terminal = new()
    {
        EntityType = nameof(Domain.Entities.Terminal),
        Group = MasterDataGroup.ReferenceList,
        TableName = "terminals",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "UN/LOCODE"),
            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // The first non-empty set in the table. Note what the live conditions are NOT: neither
        // containers nor loads carries a deleted_at column, so the usual "deleted_at IS NULL"
        // narrowing would be a runtime SQL error rather than a wrong count. The equivalent for
        // these tables is their own lifecycle status, stored as text.
        DependencyRules =
        [
            // Advisory on deactivate, blocking on delete. Closing a port to new bookings while
            // stock drains out of it is ordinary; deleting it while containers physically sit
            // there contradicts what deletion means.
            new DependencyRule(
                "containers currently at this port",
                "containers",
                "current_terminal_id",
                LiveCondition: "status <> 'returned'")
            {
                AppliesTo = [MasterDataDeactivation.Delete]
            },
            new DependencyRule(
                "containers still stationed here",
                "containers",
                "current_terminal_id",
                Blocks: false,
                LiveCondition: "status <> 'returned'")
            {
                AppliesTo = [MasterDataDeactivation.Deactivate]
            },

            // In-flight work, on the other hand, loses its origin or destination outright. Loads
            // already delivered or cancelled keep pointing here and are none of the probe's
            // business - counting them would refuse retirements nothing real depends on.
            new DependencyRule(
                "loads departing from this port",
                "loads",
                "origin_terminal_id",
                LiveCondition: "status NOT IN ('delivered', 'cancelled')"),
            new DependencyRule(
                "loads arriving at this port",
                "loads",
                "destination_terminal_id",
                LiveCondition: "status NOT IN ('delivered', 'cancelled')")
        ],

        // Looser than Branch in both directions. Real port names are long and dirty, so a high bar
        // would miss "MICT Manila Intl Container Term" against "Manila International Container
        // Terminal", and a low soft threshold is cheap when the probe only warns.
        // Provisional: these cannot be tuned until the nSFS extract lands and they can be scored
        // against TBL_PORT's actual values.
        HardThreshold = 0.88,
        SoftThreshold = 0.50
    };

    /// <summary>
    /// A ship. The first entity in the module with no legacy table behind it at all - nSFS keeps
    /// the vessel as free text on <c>TBL_CUSTOMER_SERVICE</c> and TrackWise copied that onto
    /// <c>Workfile.VesselName</c> - so the duplicate probe is not tidying an existing list here,
    /// it is the only thing standing between the new one and the same free-text sprawl.
    /// </summary>
    public static readonly MasterDataPolicy Vessel = new()
    {
        EntityType = nameof(Domain.Entities.Vessel),
        Group = MasterDataGroup.ReferenceList,
        TableName = "vessels",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),

            // The strongest signal the module has anywhere: an IMO number is globally unique and is
            // never reassigned, so two records sharing one are the same ship however differently
            // they are spelled. Worth an exact rule of its own rather than leaving the name to
            // carry a match the name is bad at.
            new DuplicateRule("ImoNumber", "imo_number", DuplicateMatchKind.Exact, "IMO number"),

            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // Nothing holds a vessel by foreign key yet. Workfile still carries VesselName as text, and
        // swapping that for a reference belongs with the workfile module rather than here - so this
        // list is empty as a recorded decision, not an omission. It stops being empty the day the
        // first FK lands, and the probe reads as "nothing depends on this" until then.
        DependencyRules = [],

        // Tighter than ports on the hard bar and looser on the soft one. Ship names are short, so
        // similarity scores move fast - "MV Ever Given" against "MV Ever Golden" already scores
        // high - and a short name makes a false block more likely than a missed duplicate. The IMO
        // rule is what catches the real duplicates; the name rule is only meant to warn.
        HardThreshold = 0.92,
        SoftThreshold = 0.45
    };

    /// <summary>
    /// What is being moved. Same free-text origin as <see cref="Vessel"/> - today "electronics",
    /// "Electronic parts" and "ELECT." are three commodities and nothing can be counted by any of
    /// them.
    /// </summary>
    public static readonly MasterDataPolicy Commodity = new()
    {
        EntityType = nameof(Domain.Entities.Commodity),
        Group = MasterDataGroup.ReferenceList,
        TableName = "commodities",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),
            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")

            // HsCode is deliberately absent. It classifies a family of goods for customs rather
            // than identifying a record, so several genuinely distinct commodity lines share one -
            // an exact rule on it would flag every one of them and train stewards to click through.
        ],

        // Empty for the same recorded reason as Vessel: Workfile.Commodity is still text.
        DependencyRules = [],

        // The strictest name matching in the table, because commodity names are built from a small
        // vocabulary of generic words. "Frozen Fish" and "Frozen Fruit" are unrelated goods that
        // score well over 0.7 on trigrams, so anything looser would block real records.
        HardThreshold = 0.95,
        SoftThreshold = 0.55
    };

    /// <summary>
    /// A place that is not a port - delivery point, warehouse, yard, or the zone grouping them.
    /// The first entity that can depend on <i>itself</i>: retiring a zone is refused while delivery
    /// points still sit inside it.
    /// </summary>
    public static readonly MasterDataPolicy Location = new()
    {
        EntityType = nameof(Domain.Entities.Location),
        Group = MasterDataGroup.ReferenceList,
        TableName = "locations",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),
            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // Self-referencing, which is what this entity adds to the module. The same declaration
        // serves the merge: MasterDataReferenceRepointer reads it to move a merged zone's children
        // onto the survivor, so a zone folded into another does not strand the points beneath it.
        DependencyRules =
        [
            new DependencyRule(
                "locations inside this zone",
                "locations",
                "parent_location_id",
                LiveCondition: "deleted_at IS NULL"),

            // Ports' rule for the same reason: only in-flight loads lose something. The merge
            // reads these too, so folding a duplicate outlet away moves its loads onto the survivor.
            new DependencyRule(
                "loads picking up here",
                "loads",
                "origin_site_id",
                LiveCondition: "status NOT IN ('delivered', 'cancelled')"),
            new DependencyRule(
                "loads delivering here",
                "loads",
                "destination_site_id",
                LiveCondition: "status NOT IN ('delivered', 'cancelled')")
        ],

        // Ports' numbers, for ports' reason: the legacy delivery-location names are long, dirty and
        // abbreviated inconsistently, which is the class of duplicate exact matching never catches.
        HardThreshold = 0.88,
        SoftThreshold = 0.50
    };

    /// <summary>
    /// A container or equipment type. The first entity in the table that <b>reconciles</b> rather
    /// than adds: two enums described these things in two vocabularies on two aggregates, and this
    /// list is the union both now resolve through.
    /// </summary>
    public static readonly MasterDataPolicy ContainerType = new()
    {
        EntityType = nameof(Domain.Entities.ContainerType),
        Group = MasterDataGroup.ReferenceList,
        TableName = "container_types",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,

        // Not branch-scoped, and the first reference list since Branch itself that is not. ISO 6346
        // does not vary by office - a 40' high cube is the same box in Manila and Cebu - so a
        // per-branch copy could only ever be the drift this module exists to remove. It therefore
        // does not appear in Branch's dependency rules, and
        // EveryBranchScopedEntity_IsDeclaredAsABranchDependency is what keeps that honest.
        BranchScoped = false,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),

            // The vessel IMO of this list: an ISO size-type code is defined by the standard, so two
            // records carrying one are the same box however each was spelled. It earns an exact
            // rule of its own rather than leaving the match to a name the name is bad at - "40' HC",
            // "40 High Cube" and "FORTY HC" all score poorly against each other and all mean 45G1.
            new DuplicateRule("IsoSizeTypeCode", "iso_size_type_code", DuplicateMatchKind.Exact, "ISO size-type code"),

            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // The first reference list with dependents in three different tables, and the payoff for
        // converting the enum columns: before this, nothing could answer "what is still using a
        // 40' reefer" because the answer lived in two enums nobody could join on.
        DependencyRules =
        [
            // Physical boxes. Blocking on delete only, for the same reason ports are: stock
            // draining out of a type the company is retiring is ordinary, deleting the type while
            // containers of it are on the yard is not.
            new DependencyRule(
                "containers of this type",
                "containers",
                "container_type_id",
                LiveCondition: "status <> 'returned'")
            {
                AppliesTo = [MasterDataDeactivation.Delete]
            },
            new DependencyRule(
                "containers of this type still in circulation",
                "containers",
                "container_type_id",
                Blocks: false,
                LiveCondition: "status <> 'returned'")
            {
                AppliesTo = [MasterDataDeactivation.Deactivate]
            },

            // Commitments, which lose their meaning outright rather than draining. Bookings that
            // already completed or were cancelled keep pointing here and are none of the probe's
            // business - counting them would refuse retirements nothing real depends on.
            new DependencyRule(
                "bookings for this equipment",
                "bookings",
                "container_type_id",
                LiveCondition: "status NOT IN ('completed', 'cancelled')"),

            // A tariff priced against a type that no longer resolves cannot be re-quoted, and a
            // superseded tariff still has to re-price history identically, so the narrowing is on
            // is_active rather than on a soft-delete column tariffs do not have.
            new DependencyRule(
                "tariffs priced for this equipment",
                "tariffs",
                "container_type_id",
                LiveCondition: "is_active"),

            // History rather than stock: a file's container slots must keep naming their size, so
            // these block a delete; retiring the size is fine and only worth a mention.
            new DependencyRule(
                "workfile containers of this size",
                "workfile_containers",
                "container_type_id")
            {
                AppliesTo = [MasterDataDeactivation.Delete]
            }
        ],

        // Looser than commodities and tighter than ports. Type names are short and built from a
        // small vocabulary - "20' Reefer" against "40' Reefer" scores high on trigrams and they are
        // different boxes - but the ISO rule is what catches the duplicates that matter, so the
        // name rule only has to warn.
        HardThreshold = 0.92,
        SoftThreshold = 0.50
    };

    /// <summary>
    /// A currency. The list alone - the FX rates the brief asks for are effective-dated, and
    /// effective dating is the rates step's machinery, so <see cref="MasterDataPolicy.EffectiveDated"/>
    /// stays false here rather than setting a flag nothing reads.
    /// </summary>
    public static readonly MasterDataPolicy Currency = new()
    {
        EntityType = nameof(Domain.Entities.Currency),
        Group = MasterDataGroup.ReferenceList,
        TableName = "currencies",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,

        // Global, for ContainerType's reason: ISO 4217 does not vary by office.
        BranchScoped = false,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "ISO 4217 code"),
            new DuplicateRule("NumericCode", "numeric_code", DuplicateMatchKind.Exact, "ISO 4217 numeric code"),
            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // ⚠️ Empty, and NOT for Vessel's reason. Vessel has no dependents; this one has thousands
        // the probe structurally cannot see. Every Money in the schema carries its currency as a
        // three-letter string, and a DependencyRule matches a foreign-key column against the
        // record's id - there is no column anywhere holding a currency's Guid for it to count.
        // Retiring a currency invoices are still denominated in therefore reads as safe, and will
        // keep reading as safe until either those columns become references or the rule shape
        // grows a way to match on a record's code. Written down because a reader who assumes this
        // list means "nothing depends on a currency" would be badly wrong.
        DependencyRules = [],

        // The strictest bar in the table, and the only one where the fuzzy rule is nearly
        // decorative: currency names come from a fixed vocabulary where "Dollar" appears a dozen
        // times, so the two exact rules do the work and the name rule must not block "Canadian
        // Dollar" against "US Dollar".
        HardThreshold = 0.95,
        SoftThreshold = 0.55
    };

    /// <summary>
    /// A trading counterparty, kept on the existing <see cref="Domain.Entities.BusinessPartner"/>.
    /// The first entry in this table that asks for a second pair of eyes, and the reason the column
    /// has been sitting here unused since the module started.
    /// <para>
    /// What makes a partner different from every list above it is not that it is harder to get
    /// right - it is what a wrong one costs. A misspelled port is a correction; a partner carries a
    /// TIN onto a forwarded charge and an address onto a bill of lading, and both are wrong in
    /// somebody else's system before anybody here notices.
    /// </para>
    /// </summary>
    public static readonly MasterDataPolicy BusinessPartner = new()
    {
        EntityType = nameof(Domain.Entities.BusinessPartner),
        Group = MasterDataGroup.BusinessPartner,
        TableName = "business_partners",

        // The first non-None approval in the table. Everything it needs already existed as a
        // concept - MasterDataApprovalMode, MasterDataState.Draft and .Pending - and none of it had
        // an implementation until this row.
        Approval = MasterDataApprovalMode.MakerChecker,

        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),

            // The vessel IMO of this list. A TIN is issued once per legal entity, so two records
            // carrying the same one are the same company however differently somebody spelled its
            // trading name - which is the duplicate a name rule is worst at, because a company
            // trades under several names on purpose. Stored digits-only so the punctuation a TIN is
            // printed with cannot hide the match.
            new DuplicateRule("TaxId", "tax_id", DuplicateMatchKind.Exact, "TIN"),

            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // Five columns on one table, because a workfile names its counterparties by slot rather
        // than through a join table - so "who still uses this partner" is five questions, and the
        // labels have to say which slot or the steward is told a number they cannot act on.
        //
        // Narrowed to files that are still moving. A closed or cancelled file keeps pointing here
        // and is none of the probe's business: counting settled history would refuse the retirement
        // of every partner the company ever stopped trading with, which is all of them eventually.
        DependencyRules =
        [
            new DependencyRule(
                "open files naming this shipping line",
                "workfiles",
                "shipping_line_id",
                LiveCondition: "status NOT IN ('closed', 'cancelled')"),
            new DependencyRule(
                "open files naming this co-loader",
                "workfiles",
                "co_loader_id",
                LiveCondition: "status NOT IN ('closed', 'cancelled')"),
            new DependencyRule(
                "open files naming this forwarder",
                "workfiles",
                "forwarder_id",
                LiveCondition: "status NOT IN ('closed', 'cancelled')"),
            new DependencyRule(
                "open files naming this trucker",
                "workfiles",
                "trucker_id",
                LiveCondition: "status NOT IN ('closed', 'cancelled')"),
            new DependencyRule(
                "open files naming this broker",
                "workfiles",
                "broker_id",
                LiveCondition: "status NOT IN ('closed', 'cancelled')")
        ],

        // Ports' numbers, for ports' reason: partner names are long and filthy - "Maersk Filipinas
        // Inc.", "MAERSK FILIPINAS", "Maersk Filipinas Incorporated" - which is exactly the class of
        // duplicate exact matching never catches. The TIN rule catches the ones that matter; the
        // name rule has to be loose enough to warn on the rest.
        // Provisional, like every other pair here: they cannot be tuned until the nSFS and
        // accounting extracts land and they can be scored against tblR_BPMasterData's real values.
        HardThreshold = 0.88,
        SoftThreshold = 0.50
    };

    /// <summary>
    /// A trailer. Same shape as <see cref="Vessel"/> - no legacy table, no dependents yet - because
    /// nothing in the domain points at a trailer by foreign key until Truck's own promotion wires
    /// load/trip assignment through it.
    /// </summary>
    public static readonly MasterDataPolicy Trailer = new()
    {
        EntityType = nameof(Domain.Entities.Trailer),
        Group = MasterDataGroup.FleetAndPersonnel,
        TableName = "trailers",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),

            // The vessel-IMO role for this list: a VIN is issued once and never reassigned, so two
            // records sharing one are the same physical trailer however differently the plate or
            // name was entered. Nullable - not every trailer has one on file.
            new DuplicateRule("Vin", "vin", DuplicateMatchKind.Exact, "VIN"),

            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // Nothing holds a trailer by foreign key yet - recorded as a decision, not an omission. It
        // stops being empty once Truck's own promotion (deferred, see MASTER_DATA_PLAN.md) wires
        // trip/load assignment through this table.
        DependencyRules = [],

        // Provisional like every other pair here: there is no legacy trailer list to score these
        // against yet.
        HardThreshold = 0.90,
        SoftThreshold = 0.50
    };

    /// <summary>
    /// A helper - a laborer who assists a driver. Deliberately not built on <c>Employee</c>; see the
    /// entity's own doc comment for why. Same shape as <see cref="Vessel"/> otherwise: no legacy
    /// table, no dependents yet.
    /// </summary>
    public static readonly MasterDataPolicy Helper = new()
    {
        EntityType = nameof(Domain.Entities.Helper),
        Group = MasterDataGroup.FleetAndPersonnel,
        TableName = "helpers",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),
            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],
        DependencyRules = [],
        HardThreshold = 0.90,
        SoftThreshold = 0.50
    };

    /// <summary>
    /// A truck, kept on the existing <see cref="Domain.Entities.Truck"/> - the same "promote, don't
    /// shadow" call <see cref="Terminal"/> made, but with a much bigger existing surface (~59
    /// Application-layer call sites versus Terminal's handful) since a truck has been a first-class
    /// entity here from the start rather than a promoted enum or free-text field.
    /// </summary>
    public static readonly MasterDataPolicy Truck = new()
    {
        EntityType = nameof(Domain.Entities.Truck),
        Group = MasterDataGroup.FleetAndPersonnel,
        TableName = "trucks",
        CodeColumn = "number",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            // "Code" is the field name every write command/DTO uses (the module's convention), but
            // Truck exposes it under its existing name - see Truck.Number's doc comment - so the SQL
            // column here is "number", not "code".
            new DuplicateRule("Code", "number", DuplicateMatchKind.Exact, "unit number"),

            // The vessel-IMO role for this list: a VIN is issued once and never reassigned, so two
            // records sharing one are the same physical truck however differently the unit number
            // or plate was entered. Nullable - not every legacy truck has one on file.
            new DuplicateRule("Vin", "vin", DuplicateMatchKind.Exact, "VIN"),

            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")
        ],

        // Unlike Trailer/Helper's empty lists, a truck already has real dependents. Narrowed to
        // work still in flight, the same way Terminal's rules are: a truck's own history (documents,
        // expenses, DVIRs, accidents, maintenance records, IFTA mileage, ELD mappings, fuel-card
        // transactions, load-board postings) is deliberately NOT declared here - retiring a truck
        // must not be blocked by the history soft delete already preserves. Only current, still-live
        // work blocks it.
        DependencyRules =
        [
            new DependencyRule(
                "loads currently assigned to this truck",
                "loads",
                "assigned_truck_id",
                LiveCondition: "status NOT IN ('delivered', 'cancelled')")
            {
                AppliesTo = [MasterDataDeactivation.Delete]
            },
            new DependencyRule(
                "loads still assigned to this truck",
                "loads",
                "assigned_truck_id",
                Blocks: false,
                LiveCondition: "status NOT IN ('delivered', 'cancelled')")
            {
                AppliesTo = [MasterDataDeactivation.Deactivate]
            },
            new DependencyRule(
                "an in-progress trip on this truck",
                "trips",
                "truck_id",
                LiveCondition: "status NOT IN ('completed', 'cancelled')")
            {
                AppliesTo = [MasterDataDeactivation.Delete]
            },
            new DependencyRule(
                "trips still assigned to this truck",
                "trips",
                "truck_id",
                Blocks: false,
                LiveCondition: "status NOT IN ('completed', 'cancelled')")
            {
                AppliesTo = [MasterDataDeactivation.Deactivate]
            }
        ],

        // The history the rules above leave out, so a merged-away duplicate's fuel expenses and
        // papers show on the survivor. ELD mappings and load-board postings are not here: each is
        // unique per (provider, truck), so they stay on the tombstone rather than collide.
        MergeReferences =
        [
            new MergeReference("expenses", "truck_expense_truck_id"),
            new MergeReference("expenses", "truck_id"),
            new MergeReference("fuel_cards", "truck_id"),
            new MergeReference("fuel_card_transactions", "truck_id"),
            new MergeReference("vehicle_permits", "truck_id"),
            new MergeReference("cash_fund_expenses", "truck_id"),
            new MergeReference("documents", "truck_id"),
            new MergeReference("maintenance_records", "truck_id"),
            new MergeReference("maintenance_schedules", "truck_id"),
            new MergeReference("dvir_reports", "truck_id"),
            new MergeReference("incidents", "truck_id"),
            new MergeReference("trip_rescues", "truck_id"),
            new MergeReference("driver_behavior_events", "truck_id"),
            new MergeReference("truck_jurisdiction_mileage", "truck_id"),
            new MergeReference("truck_location_history", "truck_id"),
            new MergeReference("agent_decisions", "truck_id")
        ],

        // Provisional like every other pair here: no legacy truck list exists to score these
        // against yet. Looser than commodities, tighter than ports - unit numbers are short like
        // ship names, so the VIN rule carries the real duplicates and the name rule only warns.
        HardThreshold = 0.90,
        SoftThreshold = 0.50
    };

    /// <summary>
    /// A tenant employee, kept on the existing <see cref="Domain.Entities.Employee"/> - the same
    /// "promote, don't shadow" call <see cref="Terminal"/> and <see cref="Truck"/> made. The hardest
    /// promotion in the module: <c>Employee</c> shares its primary key with the master-database
    /// <c>User</c>, carries payroll/Stripe Connect/invitation machinery, and - unlike Trailer/Helper -
    /// already has real dependents, so its <see cref="DependencyRules"/> cannot stay empty the way
    /// theirs do.
    /// </summary>
    public static readonly MasterDataPolicy Employee = new()
    {
        EntityType = nameof(Domain.Entities.Employee),
        Group = MasterDataGroup.FleetAndPersonnel,
        TableName = "employees",
        Approval = MasterDataApprovalMode.None,
        EffectiveDated = false,
        BranchScoped = true,
        DuplicateRules =
        [
            new DuplicateRule("Code", "code", DuplicateMatchKind.Exact, "code"),
            new DuplicateRule("Name", "name", DuplicateMatchKind.Fuzzy, "name")

            // No VIN/IMO/TIN-equivalent exists for a person - Code and Name carry the whole rule,
            // the same pair Branch uses.
        ],

        // Unlike Trailer/Helper's empty lists, an employee already has real dependents. Narrowed to
        // current assignment, not history: driver licenses, time entries, payroll invoices and
        // employee documents are deliberately NOT declared here - retiring an employee must not be
        // blocked by their own history, the same reasoning Truck's own rules use. Only being someone's
        // CURRENT truck driver or dispatcher blocks it.
        DependencyRules =
        [
            new DependencyRule(
                "trucks currently assigning this employee as main driver",
                "trucks",
                "main_driver_id",
                LiveCondition: "deleted_at IS NULL")
            {
                AppliesTo = [MasterDataDeactivation.Delete]
            },
            new DependencyRule(
                "trucks still assigning this employee as main driver",
                "trucks",
                "main_driver_id",
                Blocks: false,
                LiveCondition: "deleted_at IS NULL")
            {
                AppliesTo = [MasterDataDeactivation.Deactivate]
            },
            new DependencyRule(
                "trucks currently assigning this employee as secondary driver",
                "trucks",
                "secondary_driver_id",
                LiveCondition: "deleted_at IS NULL")
            {
                AppliesTo = [MasterDataDeactivation.Delete]
            },
            new DependencyRule(
                "trucks still assigning this employee as secondary driver",
                "trucks",
                "secondary_driver_id",
                Blocks: false,
                LiveCondition: "deleted_at IS NULL")
            {
                AppliesTo = [MasterDataDeactivation.Deactivate]
            },

            // Advisory only, on both transitions: a dispatcher moving on shouldn't block their own
            // retirement, but a steward should still be told which open loads name them.
            new DependencyRule(
                "loads dispatched by this employee",
                "loads",
                "assigned_dispatcher_id",
                Blocks: false,
                LiveCondition: "status NOT IN ('delivered', 'cancelled')")
        ],

        // Provisional like every other pair here. People's names collide more than any other list -
        // common first/last name combinations are common - so a name match alone should warn, not
        // block; Code is what a steward actually keys off.
        HardThreshold = 0.90,
        SoftThreshold = 0.50
    };

    public static readonly IReadOnlyList<MasterDataPolicy> All =
        [Branch, Terminal, Vessel, Commodity, Location, ContainerType, Currency, BusinessPartner, Trailer, Helper, Truck, Employee];
}
