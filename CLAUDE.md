# CLAUDE.md

Multi-tenant logistics platform: freight forwarding (domestic, import, export, triangular, reefer) plus trucking dispatch/fleet management, and the back-office that runs alongside it (accounting sync, HR/employees, admin, reporting). One tenant per company, one database per tenant.

Trucking dispatch (Load/Trip/Truck) was the original core and is still fully built out. Freight forwarding (Quotation → Booking → Workfile) and Master Data Management are the actively-growing side, built around one anchor tenant's real freight-forwarding operation (Philippines, multiple branches) migrating off a legacy in-house system. The target shape for that migration - a single centralized system covering Domestic/Import/Export/Triangular/Reefer/Trucking plus Admin/Accounting/HR/Reports as peer modules - is the reference for where Forwarding + MDM are headed; don't assume it's already fully built. See [MASTER_DATA_PLAN.md](MASTER_DATA_PLAN.md) for the concrete scope, build order, and what's still missing.

## How guidance is organized

| Where                                                                              | What it holds                                                                                                                                                                                                                               | When to read it                                                                                     |
| ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| **This file**                                                                      | Repo-wide architecture, commands, ports                                                                                                                                                                                                     | Always                                                                                              |
| [.claude/feature-map.md](.claude/feature-map.md)                                   | Every feature → its entity, handlers, services, pages                                                                                                                                                                                       | **Before grepping for a feature.** Update it when you add or move a top-level feature               |
| [MASTER_DATA_PLAN.md](.claude/handoff/MASTER_DATA_PLAN.md)                         | Freight-forwarding / Master Data Management plan: scope, build order, decisions locked, open blockers                                                                                                                                       | Before working on Master Data, Freight Forwarding, or Workfile                                      |
| [FORWARDING_LEGS_PLAN.md](.claude/handoff/FORWARDING_LEGS_PLAN.md)                 | Forwarding (Domestic/Import/Export/Triangular): containers on a workfile, truck legs as child Trucking workfiles, one bill per move, build order                                                                                            | Before working on forwarding workfiles, containers, door-to-door moves or the Intermodal menu       |
| [SKYBEST_CONTEXT.md](.claude/handoff/SKYBEST_CONTEXT.md)                           | Scope (TrackWise replaces nSFS for Skybest Logistics Corp. only), SLC's org and branches, the systems it runs today, IT's planned projects mapped to TrackWise, and where the nSFS source and DB backups live                               | Before scoping nSFS migration work or anything Skybest-, Monday- or IT-roadmap-level                |
| [MONDAY_OPERATIONS_CONTEXT.md](.claude/handoff/MONDAY_OPERATIONS_CONTEXT.md)       | What Skybest's operations run on in Monday.com today (Cold Chain, container trucking, forwarding documentation, fleet): real boards, fields, statuses and rules from a full API export, mapped to TrackWise, plus how to refresh the export | Before designing any Cold Chain, trucking, workfile-milestone or fleet feature                      |
| [NSFS_CONTEXT.md](.claude/handoff/NSFS_CONTEXT.md)                                 | How nSFS works (access codes, core tables, reference numbers, flaws), SLC's domain glossary, and the requirements direction for its replacement (multi-leg door-to-door shipments)                                                          | Before designing anything that replaces an nSFS module or ETLs its data                             |
| [NSFS_DATABASE.md](.claude/handoff/NSFS_DATABASE.md)                               | The live nSFS database, verified: reference numbers and counters, the quotation → booking → job model, Reefer drop tables, payables, status values, branches, users, what is dead                                                           | Before querying nSFS, planning an ETL or import from it, or continuing any nSFS numbering           |
| [COLD_CHAIN_LOGISTICS_CONTEXT.md](.claude/handoff/COLD_CHAIN_LOGISTICS_CONTEXT.md) | Ground truth on how Skybest's Cold Chain Logistics (CCL) ops actually run today (Monday.com), reverse-engineered from real board exports - not built here yet                                                                               | Before working on Cold Chain, Reefer, or Genset features                                            |
| [COLD_CHAIN_MIGRATION_PLAN.md](.claude/handoff/COLD_CHAIN_MIGRATION_PLAN.md)       | How Cold Chain moves off Monday + nSFS onto TrackWise: the target client lifecycle, the phased rollout (which Monday boards each phase replaces), decisions made and open questions                                                         | Before building or scheduling any Cold Chain phase work                                             |
| Nested `CLAUDE.md`                                                                 | Local guidance for a subtree, e.g. [the Angular workspace](src/Client/Trackwise.Angular/CLAUDE.md)                                                                                                                                          | When working in that directory                                                                      |
| [.claude/rules/](.claude/rules/)                                                   | Conventions and traps, scoped by `paths:` frontmatter                                                                                                                                                                                       | Auto-loaded by Claude Code when you touch a matching file. **Other agents must read them manually** |
| [.claude/skills/](.claude/skills/)                                                 | Step-by-step recipes for multi-file tasks (feature slice, webhook, job, provider, permission, migration)                                                                                                                                    | Before hand-rolling a task a skill already covers                                                   |
| [docs/](docs/index.md)                                                             | Deep dives - [architecture](docs/architecture/overview.md), [multi-tenancy](docs/architecture/multi-tenancy.md), [domain model](docs/architecture/domain-model.md), [AI dispatch](docs/ai-dispatch.md), [roadmap](docs/roadmap/README.md)   | When you need the full picture, not just the location                                               |

## Build & Run

```bash
# Local dev infrastructure (Postgres; runs the migrator once) - requires Docker
docker compose -f deploy/docker-compose.dev.yml up -d     # Postgres: 5433

# Backend only
dotnet build                                              # Build all
dotnet test                                               # Run all tests
dotnet test --filter "ClassName"                          # Filter by class
dotnet run --project src/Presentation/Trackwise.IdentityServer  # Identity: https://localhost:7001
dotnet run --project src/Presentation/Trackwise.API       # API: https://localhost:7000

# Frontend (Angular workspace, bun)
bun install
bun start:tms                                             # TMS Portal dev server

# Mobile (Kotlin Multiplatform)
cd src/Client/Trackwise.DriverApp && ./gradlew assembleDebug
```

## Service Ports

| Service         | Port |
| --------------- | ---- |
| API             | 7000 |
| Identity Server | 7001 |
| Admin Portal    | 7002 |
| TMS Portal      | 7003 |
| Customer Portal | 7004 |
| Website         | 7005 |

## Architecture (first-pass facts)

- **DDD + CQRS**: Commands/Queries via MediatR in `src/Core/Trackwise.Application/`. Requests implement `ICommand<T>` or `IQuery<T>` (in `Application.Abstractions/Common/`); handlers own their `SaveChangesAsync` calls (no auto-transaction wrapper)
- **Multi-tenant**: Master DB (tenants, subscriptions) + one DB per tenant. Tenant resolved per-request via `CurrentTenantAccessor` (`ICurrentTenantAccessor`) (priority: MCP API key → `X-Tenant` header → JWT claim)
- **Lazy loading**: EF Core lazy loading enabled - do NOT use `.Include()` for navigation properties. The flip side: reading a navigation property inside a mapper or a list loop is an N+1. Batch the lookup and pass the value in (see [mapperly.md](.claude/rules/backend/mapperly.md))
- **Modular infrastructure**: 14 focused projects under `src/Infrastructure/` (see [overview.md](docs/architecture/overview.md)). Shared HTTP-JSON plumbing, webhook signature validation (`WebhookSignature`), and the provider factory base for the third-party providers live in `Integrations.Common` - do NOT hand-roll a fourth copy
- **Hangfire jobs bypass the MediatR pipeline**, so `[RequiresFeature]` is inert there. A job must check `IFeatureService` itself, and should fan out via `TenantJobRunner.ForEachTenantAsync` (`src/Presentation/Trackwise.API/Jobs/`)

### Layer boundaries

Application references `Trackwise.Application.Abstractions` for infrastructure ports; workflow services stay in `Trackwise.Application`. Infrastructure projects depend on `Application.Abstractions` only - **never** on `Application`. The composition root is each host's `Setup.cs` (`ConfigureServices`); `Program.cs` is a thin `TrackwiseHost.Run` shell (`src/Presentation/Trackwise.HostDefaults/`).

`test/Trackwise.Architecture.Tests/` enforces this by **discovering** projects off disk - never reintroduce an `InlineData` roster there, since a hand-maintained list silently skips whatever nobody remembered to add.

Adding an infrastructure project: the csproj rule finds it automatically, but the IL-level boundary rule also needs an anchor in `AssemblyAnchors.AllInfrastructure` **plus** a `ProjectReference` in the arch-tests csproj. Miss those and the project is simply unchecked - nothing fails.

## User Roles

`SuperAdmin`, `Admin`, `Owner`, `Manager`, `Dispatcher`, `Driver`, `Customer`

## MCP Server (high-friction details)

- Endpoint: `/mcp` (Streamable HTTP)
- Auth: API key header, format `logsx_{tenantId}_{random}`. Validated by `ApiKeyAuthenticationHandler`, which sets `HttpContext.Items["McpTenantId"]` so `CurrentTenantAccessor` resolves the tenant without an `X-Tenant` header
- Rate limit: 100 req/min per key
- Tools come from `AgentToolCatalog`, which discovers them from the tool classes and is shared with the AI dispatch agent. Add a tool class and every surface picks it up
- Project: `src/Presentation/Trackwise.McpServer/`

# Github

- Upon pushing / committing, do not add claude code as a co-author.
