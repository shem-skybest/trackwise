using Trackwise.Domain.Entities;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The policy table is the module's answer to per-entity behaviour: adding an entity means adding a
/// row, never a branch in a handler. These guard the invariants every entry has to satisfy, so a
/// new entry that breaks one fails here rather than at runtime on a steward's screen.
/// </summary>
public class MasterDataPolicyTests
{
    private readonly IMasterDataPolicyProvider sut = new MasterDataPolicyProvider();

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        // Callers pass a CLR type name, but route values and import files arrive in any casing.
        Assert.NotNull(sut.Find("branch"));
        Assert.NotNull(sut.Find("Branch"));
        Assert.NotNull(sut.Find("BRANCH"));
    }

    [Fact]
    public void Find_UnknownEntity_ReturnsNull()
    {
        // A null policy is how the write pipeline decides an entity is simply not governed.
        Assert.Null(sut.Find("Spaceship"));
    }

    [Fact]
    public void EveryPolicy_HasAtLeastOneDuplicateRule()
    {
        // A policy with no rules silently disables duplicate detection for that entity - the one
        // failure mode that looks exactly like "no duplicates found".
        Assert.All(sut.All, policy => Assert.NotEmpty(policy.DuplicateRules));
    }

    [Fact]
    public void EveryPolicy_HasAHardThresholdAboveItsSoftThreshold()
    {
        // Inverted thresholds would block every candidate it merely meant to warn about.
        Assert.All(sut.All, policy =>
        {
            Assert.InRange(policy.SoftThreshold, 0, 1);
            Assert.InRange(policy.HardThreshold, 0, 1);
            Assert.True(policy.HardThreshold > policy.SoftThreshold,
                $"{policy.EntityType}: hard threshold must exceed soft threshold.");
        });
    }

    [Fact]
    public void EverySoftThreshold_ClearsThePgTrgmOperatorThreshold()
    {
        // DuplicateProbe pre-filters with the % operator so the trigram index can be used at all -
        // scoring with similarity() alone is a per-row function call no index can answer. That
        // pre-filter is a superset of the scored result only while the soft threshold is at or
        // above PostgreSQL's pg_trgm.similarity_threshold. Set one lower and the probe silently
        // stops surfacing candidates it was configured to warn about.
        const double PgTrgmDefaultThreshold = 0.3;

        Assert.All(sut.All, policy =>
            Assert.True(policy.SoftThreshold >= PgTrgmDefaultThreshold,
                $"{policy.EntityType}: a soft threshold below {PgTrgmDefaultThreshold} is excluded "
                + "by the probe's index pre-filter before it can ever be scored."));
    }

    [Fact]
    public void EveryPolicy_HasDistinctFieldNames()
    {
        // Duplicate field names would make the probe bind two parameters to one supplied value.
        Assert.All(sut.All, policy =>
            Assert.Equal(
                policy.DuplicateRules.Select(r => r.Field).Distinct().Count(),
                policy.DuplicateRules.Count));
    }

    [Fact]
    public void EveryPolicy_UsesSafeTableAndColumnIdentifiers()
    {
        // These are interpolated into the probe's SQL. They come from source, not from requests,
        // and this test is what keeps it that way if someone later makes them configurable.
        Assert.All(sut.All, policy =>
        {
            Assert.Matches("^[a-z_][a-z0-9_]*$", policy.TableName);
            Assert.All(policy.DuplicateRules, rule => Assert.Matches("^[a-z_][a-z0-9_]*$", rule.Column));
        });
    }

    [Fact]
    public void EveryDependencyRule_UsesSafeTableAndColumnIdentifiers()
    {
        // The dependency probe interpolates these the same way, so they carry the same obligation.
        Assert.All(sut.All, policy =>
            Assert.All(policy.DependencyRules, rule =>
            {
                Assert.Matches("^[a-z_][a-z0-9_]*$", rule.TableName);
                Assert.Matches("^[a-z_][a-z0-9_]*$", rule.Column);
            }));
    }

    [Fact]
    public void EveryMergeReference_UsesSafeTableAndColumnIdentifiers()
    {
        // The repointer interpolates these into its UPDATE the same way.
        Assert.All(sut.All, policy =>
            Assert.All(policy.MergeReferences, reference =>
            {
                Assert.Matches("^[a-z_][a-z0-9_]*$", reference.TableName);
                Assert.Matches("^[a-z_][a-z0-9_]*$", reference.Column);
            }));
    }

    [Fact]
    public void Truck_MergeCarriesItsExpensesAndPapers()
    {
        // Retiring a truck must not be blocked by its history, so these are not dependency rules -
        // but merging a duplicate away must still bring that history to the survivor.
        var references = sut.Find("Truck")!.MergeReferences;

        Assert.Contains(new MergeReference("expenses", "truck_expense_truck_id"), references);
        Assert.Contains(new MergeReference("fuel_card_transactions", "truck_id"), references);
        Assert.Contains(new MergeReference("vehicle_permits", "truck_id"), references);
        Assert.DoesNotContain(sut.Find("Truck")!.DependencyRules, r => r.TableName == "expenses");
    }

    [Fact]
    public void EveryDependencyRule_AppliesToAtLeastOneRetirementMode()
    {
        // A rule applying to nothing is dead configuration that reads as protection.
        Assert.All(sut.All, policy =>
            Assert.All(policy.DependencyRules, rule =>
            {
                Assert.NotEmpty(rule.AppliesTo);
                Assert.DoesNotContain(MasterDataDeactivation.None, rule.AppliesTo);
            }));
    }

    [Fact]
    public void EveryDependencyRule_HasADistinctLabel()
    {
        // Labels are read out to the steward as a list; two groups called the same thing would
        // report "3 trucks, 5 trucks" rather than one number they can act on.
        Assert.All(sut.All, policy =>
            Assert.Equal(
                policy.DependencyRules.Select(r => r.Label).Distinct().Count(),
                policy.DependencyRules.Count));
    }

    [Fact]
    public void Branch_BlocksRetirementWhileItStillOwnsAnyScopedList()
    {
        // The successor to Branch_DeclaresNoDependenciesYet, which pinned the empty list until the
        // first IBranchScoped entity landed. Ports were that entity; vessels, commodities and
        // locations have joined it. Closing an office out from under the records scoped to it would
        // leave every one of them pointing at a branch that no longer resolves.
        var rules = sut.Find("Branch")!.DependencyRules;

        // Every rule points at the same column, because that is what branch scoping is: one nullable
        // branch_id per scoped table. Asserting the set rather than a single rule is what stops this
        // test quietly passing once a second scoped entity lands.
        Assert.Contains(rules, r => r.TableName == "terminals");
        Assert.All(rules, rule =>
        {
            Assert.Equal("branch_id", rule.Column);
            Assert.True(rule.Blocks);
        });
    }

    [Fact]
    public void EveryBranchScopedEntity_IsDeclaredAsABranchDependency()
    {
        // The reciprocal of the rule above, and the reason it generalises past ports. An entity
        // that carries a branch_id without appearing in Branch's dependency rules fails silently:
        // the probe finds nothing and retiring the branch looks safe. This is what turns that into
        // a build failure the next time an IBranchScoped entity is added.
        var branchDependencies = sut.Find("Branch")!.DependencyRules
            .Select(r => r.TableName)
            .ToHashSet();

        Assert.All(
            sut.All.Where(p => p.BranchScoped),
            policy => Assert.Contains(policy.TableName, branchDependencies));
    }

    [Fact]
    public void Terminal_IsABranchScopedDirectSave()
    {
        var policy = sut.Find("Terminal")!;

        // Ports are maintained by forwarding and documentation without a second signature; approval
        // arrives with partners and rates, where a wrong value has a price attached.
        Assert.Equal(MasterDataApprovalMode.None, policy.Approval);
        Assert.False(policy.EffectiveDated);
        Assert.True(policy.BranchScoped);
    }

    [Fact]
    public void Terminal_MatchesCodeExactlyAndNameFuzzily()
    {
        var policy = sut.Find("Terminal")!;

        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "Code").Kind);

        // The reason ports are the first real test of the probe: fuzzy on name is what catches
        // "MICT Manila Intl Container Term" against "Manila International Container Terminal".
        Assert.Equal(
            DuplicateMatchKind.Fuzzy,
            policy.DuplicateRules.Single(r => r.Field == "Name").Kind);
    }

    [Fact]
    public void Terminal_BlocksDeletionOnEveryDependent()
    {
        // Deleting means "this was created by mistake", which anything still pointing at the record
        // contradicts. Every rule therefore blocks a delete, even the one that merely warns on a
        // deactivate.
        var deleteRules = sut.Find("Terminal")!.DependencyRules
            .Where(r => r.AppliesTo.Contains(MasterDataDeactivation.Delete))
            .ToList();

        Assert.Equal(3, deleteRules.Count);
        Assert.All(deleteRules, rule => Assert.True(rule.Blocks));
    }

    [Fact]
    public void Terminal_TreatsStationedContainersAsAdviceWhenDeactivating()
    {
        // Closing a port to new bookings while stock drains out of it is ordinary operations, so
        // the container count informs rather than obstructs. A probe that refuses everything gets
        // overridden by reflex, which costs more than it saves.
        var rule = Assert.Single(
            sut.Find("Terminal")!.DependencyRules,
            r => r.AppliesTo.Contains(MasterDataDeactivation.Deactivate) && r.TableName == "containers");

        Assert.False(rule.Blocks);
    }

    [Fact]
    public void EveryLiveCondition_NamesAColumnItsOwnTableHas()
    {
        // A live condition is interpolated raw, so naming a column the dependent table lacks is a
        // runtime SQL error rather than a wrong count - and it surfaces on a steward's retirement,
        // not here. "deleted_at IS NULL" is the habitual choice and is wrong for most tables:
        // soft delete is opt-in per entity, and neither containers nor loads carries the column.
        var softDeletable = new[]
        {
            "terminals", "branches", "vessels", "commodities", "locations",
            "container_types", "currencies", "business_partners", "trailers", "helpers",
            "trucks", "employees"
        };

        Assert.All(sut.All, policy =>
            Assert.All(policy.DependencyRules, rule =>
            {
                if (rule.LiveCondition?.Contains("deleted_at") == true)
                {
                    Assert.Contains(rule.TableName, softDeletable);
                }
            }));
    }

    [Fact]
    public void Branch_IsDirectSave_AndNotBranchScoped()
    {
        var policy = sut.Find("Branch")!;

        // A branch is a low-impact lookup, and a branch cannot belong to a branch.
        Assert.Equal(MasterDataApprovalMode.None, policy.Approval);
        Assert.False(policy.RequiresApproval);
        Assert.False(policy.BranchScoped);
        Assert.False(policy.EffectiveDated);
    }

    [Fact]
    public void Branch_MatchesCodeExactlyAndNameFuzzily()
    {
        var policy = sut.Find("Branch")!;

        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "Code").Kind);

        // Fuzzy on name is what catches "Manila Head Ofice" against "Manila Head Office".
        Assert.Equal(
            DuplicateMatchKind.Fuzzy,
            policy.DuplicateRules.Single(r => r.Field == "Name").Kind);
    }

    [Fact]
    public void Vessel_MatchesTheImoNumberExactlyAsWellAsTheCode()
    {
        var policy = sut.Find("Vessel")!;

        // The strongest signal the module has anywhere: an IMO number is globally unique and never
        // reassigned, so two records sharing one are the same ship however differently they are
        // spelled. Leaving that to the fuzzy name rule would miss it on every renamed vessel.
        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "ImoNumber").Kind);

        Assert.Equal(
            DuplicateMatchKind.Fuzzy,
            policy.DuplicateRules.Single(r => r.Field == "Name").Kind);
    }

    [Fact]
    public void Vessel_MatchesNamesStrictlyBecauseShipNamesAreShort()
    {
        // Short names move trigram scores fast - "MV Ever Given" against "MV Ever Golden" already
        // scores high - so a hard bar as loose as the port list would refuse real ships. The IMO
        // rule catches the real duplicates; the name rule is only meant to warn.
        var vessel = sut.Find("Vessel")!;
        var terminal = sut.Find("Terminal")!;

        Assert.True(vessel.HardThreshold > terminal.HardThreshold);
    }

    [Fact]
    public void Commodity_MatchesNamesMostStrictlyOfAll()
    {
        // Commodity names are built from a small vocabulary of generic words: "Frozen Fish" and
        // "Frozen Fruit" are unrelated goods that score well over 0.7 on trigrams. Anything looser
        // here blocks records that are genuinely distinct.
        Assert.All(
            sut.All.Where(p => p.EntityType != nameof(Commodity)),
            policy => Assert.True(
                sut.Find("Commodity")!.HardThreshold >= policy.HardThreshold,
                $"Commodity should match at least as strictly as {policy.EntityType}."));
    }

    [Fact]
    public void Commodity_DoesNotTreatASharedHsCodeAsADuplicate()
    {
        // An HS code classifies a family of goods for customs rather than identifying a record, so
        // distinct commodity lines legitimately share one. An exact rule on it would flag every
        // one of them and train stewards to click through the probe.
        Assert.DoesNotContain(
            sut.Find("Commodity")!.DuplicateRules,
            rule => rule.Column == "hs_code");
    }

    [Fact]
    public void Location_DependsOnItself()
    {
        // The first entity in the table that does. Retiring a zone while delivery points still sit
        // inside it leaves them nested under something that no longer resolves - and the same
        // declaration is what MasterDataReferenceRepointer reads to move those children onto the
        // survivor when a zone is merged away.
        var rule = Assert.Single(
            sut.Find("Location")!.DependencyRules, r => r.TableName == "locations");

        Assert.Equal("parent_location_id", rule.Column);
        Assert.True(rule.Blocks);
    }

    [Fact]
    public void Location_DeclaresLoadSiteLinks()
    {
        // Loads that picked a saved site. Declared so a retirement counts the in-flight ones and a
        // merge moves every load off the duplicate outlet onto the survivor.
        var columns = sut.Find("Location")!.DependencyRules
            .Where(r => r.TableName == "loads")
            .Select(r => r.Column)
            .Order()
            .ToArray();

        Assert.Equal(["destination_site_id", "origin_site_id"], columns);
    }

    [Fact]
    public void VesselAndCommodity_DeclareNoDependenciesYet()
    {
        // An empty list is a decision, not an omission - which is why DependencyRules is required.
        // Nothing holds either of these by foreign key while Workfile still carries VesselName and
        // Commodity as free text. This fails the day that changes, which is the point: swapping
        // those columns for references without declaring them here would leave the probe reporting
        // every retirement as safe.
        Assert.Empty(sut.Find("Vessel")!.DependencyRules);
        Assert.Empty(sut.Find("Commodity")!.DependencyRules);
    }

    [Fact]
    public void ContainerTypeAndCurrency_AreGlobalRatherThanBranchScoped()
    {
        // The first reference lists since Branch itself that are not scoped, and the reason is the
        // same for both: ISO 6346 and ISO 4217 do not vary by office. A per-branch copy of a 40'
        // high cube could only ever be the drift this module exists to remove.
        Assert.False(sut.Find("ContainerType")!.BranchScoped);
        Assert.False(sut.Find("Currency")!.BranchScoped);
    }

    [Fact]
    public void ContainerType_MatchesTheIsoSizeTypeCodeExactlyAsWellAsTheCode()
    {
        // Vessel's IMO rule, for a vessel's reason: an ISO size-type code is defined by the
        // standard, so two records carrying one are the same box however each was spelled. Leaving
        // it to the fuzzy name rule would miss it on every record - "40' HC", "40 High Cube" and
        // "FORTY HC" score poorly against each other and all mean 45G1.
        var policy = sut.Find("ContainerType")!;

        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "IsoSizeTypeCode").Kind);

        Assert.Equal(
            DuplicateMatchKind.Fuzzy,
            policy.DuplicateRules.Single(r => r.Field == "Name").Kind);
    }

    [Fact]
    public void ContainerType_IsDependedOnByEveryTableThatReferencesIt()
    {
        // The three tables that used to hold an enum, plus workfile container slots. Before the
        // conversion nothing could answer "what still uses a 40' reefer", because the answer lived
        // in two enums on two aggregates and nothing could join them.
        var tables = sut.Find("ContainerType")!.DependencyRules
            .Select(r => r.TableName)
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal(["bookings", "containers", "tariffs", "workfile_containers"], tables);
        Assert.All(
            sut.Find("ContainerType")!.DependencyRules,
            rule => Assert.Equal("container_type_id", rule.Column));
    }

    [Fact]
    public void ContainerType_TreatsCirculatingBoxesAsAdviceWhenDeactivating()
    {
        // Ports' reasoning, applied to equipment: retiring a type while stock drains out of it is
        // ordinary operations, so the container count informs rather than obstructs. A probe that
        // refuses everything gets overridden by reflex, which costs more than it saves.
        var rule = Assert.Single(
            sut.Find("ContainerType")!.DependencyRules,
            r => r.AppliesTo.Contains(MasterDataDeactivation.Deactivate) && r.TableName == "containers");

        Assert.False(rule.Blocks);
    }

    [Fact]
    public void Currency_MatchesTheNumericCodeExactlyAsWellAsTheAlphaCode()
    {
        var policy = sut.Find("Currency")!;

        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "Code").Kind);

        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "NumericCode").Kind);
    }

    [Fact]
    public void Currency_DeclaresNoDependenciesBecauseThereIsNoColumnToCount()
    {
        // NOT Vessel's empty list. Vessel has no dependents; a currency has thousands the probe
        // structurally cannot see - every Money in the schema carries its currency as a three-letter
        // string, and a DependencyRule matches a foreign-key column against the record's id. There
        // is no column anywhere holding a currency's Guid, so retiring one that invoices are still
        // denominated in reads as safe. This pins the gap so it stays a recorded decision rather
        // than becoming a surprise.
        Assert.Empty(sut.Find("Currency")!.DependencyRules);
    }

    [Fact]
    public void Currency_IsNotEffectiveDatedBecauseTheFxTableIsTheRatesStep()
    {
        // The whole reason currencies split out of step 6. The list is not time-bound; the rate
        // that converts between two of them is, and that machinery arrives with tariffs. Setting
        // the flag here would point at behaviour nothing implements.
        Assert.False(sut.Find("Currency")!.EffectiveDated);
    }

    [Fact]
    public void EveryLiveCondition_QuotesEnumValuesTheWayTheyAreStored()
    {
        // SnakeCaseEnumConverter writes every enum column as snake_case, so a condition comparing
        // against 'Returned' or 'Delivered' matches nothing and the narrowing quietly does not
        // narrow: the probe then counts every row in the dependent table, including the settled
        // history it was written to exclude. Silent in exactly the wrong direction - retirements
        // start being refused for containers returned two years ago.
        //
        // Both the port and container-type rules shipped with PascalCase literals and were fixed
        // with partners; this is what stops the next one.
        var literal = new System.Text.RegularExpressions.Regex("'([^']*)'");

        Assert.All(sut.All, policy =>
            Assert.All(policy.DependencyRules, rule =>
            {
                foreach (var match in literal.Matches(rule.LiveCondition ?? "").Cast<System.Text.RegularExpressions.Match>())
                {
                    var value = match.Groups[1].Value;

                    Assert.True(
                        System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z0-9_]+$"),
                        $"{policy.EntityType}/{rule.Label}: '{value}' is not how the column stores "
                        + "it. Enum columns are snake_case.");
                }
            }));
    }

    [Fact]
    public void BusinessPartner_IsTheFirstEntityThatNeedsASecondPairOfEyes()
    {
        // The column has been on MasterDataPolicy since the module started and every entry set it
        // to None. This is the entry that gives it an implementation, and the reason is not that a
        // partner is harder to get right - it is what a wrong one costs, in somebody else's system,
        // before anybody here notices.
        var policy = sut.Find("BusinessPartner")!;

        Assert.Equal(MasterDataApprovalMode.MakerChecker, policy.Approval);
        Assert.True(policy.RequiresApproval);
        Assert.True(policy.BranchScoped);
        Assert.False(policy.EffectiveDated);
        Assert.Equal(MasterDataGroup.BusinessPartner, policy.Group);
    }

    [Fact]
    public void BusinessPartner_MatchesTheTinExactlyAsWellAsTheCode()
    {
        // Vessel's IMO rule, for a partner's reason: a TIN is issued once per legal entity, so two
        // records carrying the same one are the same company however differently somebody spelled
        // its trading name - which is the duplicate a name rule is worst at, because a company
        // trades under several names on purpose.
        var policy = sut.Find("BusinessPartner")!;

        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "TaxId").Kind);

        Assert.Equal(
            DuplicateMatchKind.Fuzzy,
            policy.DuplicateRules.Single(r => r.Field == "Name").Kind);
    }

    [Fact]
    public void BusinessPartner_CountsEveryCounterpartySlotAWorkfileHas()
    {
        // A workfile names its counterparties by slot rather than through a join table, so "who
        // still uses this partner" is five questions on one table. A rule set that covered four of
        // them would report a retirement as safe while an open file still named the partner in the
        // fifth - and the labels have to differ per slot, or the steward is told a number they
        // cannot act on.
        var rules = sut.Find("BusinessPartner")!.DependencyRules;

        Assert.Equal(
            ["broker_id", "co_loader_id", "forwarder_id", "shipping_line_id", "trucker_id"],
            rules.Select(r => r.Column).Order().ToArray());

        Assert.All(rules, rule =>
        {
            Assert.Equal("workfiles", rule.TableName);
            Assert.True(rule.Blocks);

            // Settled history keeps pointing here and is none of the probe's business: counting it
            // would refuse the retirement of every partner the company ever stopped trading with,
            // which is all of them eventually.
            Assert.Equal("status NOT IN ('closed', 'cancelled')", rule.LiveCondition);
        });
    }

    [Fact]
    public void Trailer_MatchesTheVinExactlyAsWellAsTheCode()
    {
        // Vessel's IMO rule, for a trailer's reason: a VIN is issued once and never reassigned, so
        // two records sharing one are the same physical trailer however differently the rest was
        // entered.
        var policy = sut.Find("Trailer")!;

        Assert.Equal(
            DuplicateMatchKind.Exact,
            policy.DuplicateRules.Single(r => r.Field == "Vin").Kind);

        Assert.Equal(
            DuplicateMatchKind.Fuzzy,
            policy.DuplicateRules.Single(r => r.Field == "Name").Kind);
    }

    [Fact]
    public void TrailerAndHelper_AreFleetAndPersonnelAndDeclareNoDependenciesYet()
    {
        // Nothing holds either by foreign key until Truck's and Employee/Driver's own promotions
        // (deferred, see MASTER_DATA_PLAN.md) wire assignment through these tables. An empty list is
        // a decision, not an omission - the same reason Vessel and Commodity's is empty.
        Assert.Equal(MasterDataGroup.FleetAndPersonnel, sut.Find("Trailer")!.Group);
        Assert.Equal(MasterDataGroup.FleetAndPersonnel, sut.Find("Helper")!.Group);
        Assert.Empty(sut.Find("Trailer")!.DependencyRules);
        Assert.Empty(sut.Find("Helper")!.DependencyRules);
    }

    [Fact]
    public void EveryReferenceList_IsADirectSaveAndNotEffectiveDated()
    {
        // Approval arrives with partners and rates, where a wrong value has a price attached, and
        // effective dating arrives with rates alone. A reference list that quietly asked for either
        // would be waiting on machinery that does not exist yet.
        Assert.All(
            sut.All.Where(p => p.Group is MasterDataGroup.ReferenceList),
            policy =>
            {
                Assert.Equal(MasterDataApprovalMode.None, policy.Approval);
                Assert.False(policy.RequiresApproval);
                Assert.False(policy.EffectiveDated);
            });
    }
}
