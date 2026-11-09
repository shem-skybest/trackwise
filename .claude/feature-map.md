# Feature Map

Stable starting points for finding code by feature. Read this before grepping. If a feature isn't here, it doesn't exist yet (or this file is stale - flag it).

Each feature lists only the layers it touches: **Domain** (entities/VOs), **Application** (commands/queries/services), **Infrastructure** (service implementations), **API/UI** (controllers, jobs, frontend pages). A missing label means that layer has nothing feature-specific.

This file answers _where_. For _how it works_, follow the deep dive: **AI dispatch** → [docs/ai-dispatch.md](../docs/ai-dispatch.md) · **Financial** → [invoices](../docs/invoices.md), [customer payments](../docs/customer-payments.md), [Stripe Connect](../docs/stripe-connect.md), [Stripe webhooks](../docs/stripe-webhooks.md) · **MCP** → [docs/mcp-server.md](../docs/mcp-server.md) · **architecture** → [overview](../docs/architecture/overview.md), [multi-tenancy](../docs/architecture/multi-tenancy.md), [domain model](../docs/architecture/domain-model.md), [module layout](../docs/architecture/module-layout.md) · **not built yet** → [docs/roadmap/](../docs/roadmap/README.md).

## Where things go (conventions)

- **Domain entity**: `src/Core/Trackwise.Domain/Entities/{Feature}/`
- **Commands**: `src/Core/Trackwise.Application/Modules/{Module}/{Feature}/Commands/{Verb}{Entity}/`
- **Queries**: `src/Core/Trackwise.Application/Modules/{Module}/{Feature}/Queries/Get{Entity}/`
- **Domain events**: `src/Core/Trackwise.Domain/Events/`; handlers in `src/Core/Trackwise.Application/Modules/{Module}/{Feature}/Events/`
- **Domain specifications** (base + cross-feature): `src/Core/Trackwise.Domain/Specifications/`
- **Application specifications** (feature-scoped, e.g. `SearchTrucks`, `GetInvoices`): `src/Core/Trackwise.Application/Modules/{Module}/{Feature}/Specifications/`
- **Infrastructure-port interfaces** (implemented by Infrastructure): `src/Core/Trackwise.Application.Abstractions/{Domain}/`
- **Application workflow services** (stay in Application): `src/Core/Trackwise.Application/Modules/{Module}/{Feature}/Services/`
- **Application constants**: feature-scoped under `Modules/{Module}/{Feature}/Constants/`; cross-module shared in `Modules/Common/Constants/`
- **Service implementations**: `src/Infrastructure/Trackwise.Infrastructure.{Module}/`
- **Shared provider HTTP**: `Trackwise.Infrastructure.Integrations.Common/HttpClientJsonExtensions.cs` - `TryGetFromJsonAsync` (never throws; logs, returns `default`), used by LoadBoard/FuelCards/Eld. Don't copy it into a new integration. `Integrations.Accounting`'s QBO helpers deliberately **throw** so push errors surface - not duplicates, don't fold in
- **REST controllers**: `src/Presentation/Trackwise.API/Controllers/{Feature}Controller.cs`
- **SignalR hubs**: `src/Infrastructure/Trackwise.Infrastructure.Communications/SignalR/Hubs/`
- **Hangfire jobs**: `src/Presentation/Trackwise.API/Jobs/` - fan out with `TenantJobRunner.ForEachTenantAsync` and gate features inside the body (see CLAUDE.md for the canonical `[RequiresFeature]`-is-inert rule)
- **Machine-readable API errors**: `Trackwise.Shared.Models/ErrorCodes.cs`, mirrored in `projects/shared/src/lib/errors/upgrade-handler.ts` (see `.claude/rules/backend/api-design.md` for the contract)
- **Webhooks**: `WebhookController.cs` + `Application/Modules/Integrations/Webhooks/Commands/`
- **EF configurations**: `src/Infrastructure/Trackwise.Infrastructure.Persistence/Configurations/{Feature}/`
- **Frontend pages**: `src/Client/Trackwise.Angular/projects/{portal}/src/app/pages/{feature}/`
  - Portals: `tms-portal` (dispatchers), `customer-portal` (shippers), `admin-portal` (super admin), `website` (marketing)
- **Mobile (driver)**: `src/Client/Trackwise.DriverApp/composeApp/src/commonMain/kotlin/com/trackwise/driver/`
- **Provider connect dialogs**: `tms-portal/src/app/shared/components/integrations/provider-connect-dialog/` - MUST be used for every ELD / load board / fuel card "add provider" dialog (see the Angular `CLAUDE.md` for how)
- **Address inputs**: `<ui-address-form>` from `@trackwise/shared` - MUST be used by every form that captures an `Address` value object; never raw `<input>`s. Country drives the State / Region / Province label (`state-labels.ts`), State stays required everywhere. Server-side counterpart: `Trackwise.Application.Validators.AddressValidator`

## Operations

### Loads

- Domain: `Entities/Load/` - optional `BookingId` (nullable, backward compatible) links a Load to its freight-forwarding Booking; see _Booking_ under Freight Forwarding Features
- Application: `Modules/Operations/Loads/` (Commands, Queries, Services, Specifications, Constants)
- API/UI: `LoadController.cs`, `tms-portal/pages/loads/`

### Trips

- Domain: `Entities/Trip/`
- Application: `Modules/Operations/Trips/Commands/`, `Modules/Operations/Trips/Queries/`
- Infrastructure: `Routing/Optimization/` (trip optimizer)
- API/UI: `TripController.cs`, `tms-portal/pages/trips/`
- ⚠️ **Dispatching a trip dispatches its draft loads** (`Trip.Dispatch`, and `AddLoads` on a dispatched
  trip) - status and `DispatchedAt` only; their invoices stay as they are, unlike `Load.Dispatch`. The
  driver app offers pickup / delivery only on a dispatched load
- **Driver app "Today"** (2026-10-13): the dashboard lists the driver's own dispatched / in-transit
  trips (`GET /trips?driverId=`, crews rotate trucks so never by truck) as warehouse-visit and drop
  blocks built like the crew report's (`model/TripBlocks.kt`, same ~100 m same-visit rule as
  `utils/same-visit.ts`; the outlet is the part of the load name after " - "). Each block's photo goes
  through the existing POD / BOL capture with its `tripStopId`, so it lands on the crew report's photo
  strip. A warehouse photo gets a document per load of the visit (one blob, `CaptureBillOfLadingHandler`)
- **Crew trip events from the Driver App** (Phase 2, 2026-10-15, decided "both ways"): Today's blocks
  tap Arrived → Start/Done loading or unloading → Departed (+ temp, drop outcome), plus Left / Back at
  garage, through `POST /drivers/trips/{id}/events` and `/events/undo` (`RecordDriverTripEvent`,
  `UndoDriverTripEvent`, guarded by `DriverTripAccess`). They write the same fields as the crew
  report via `Trip.CrewEvents.cs`; whoever records first keeps it, each event keeps its
  `TripEventSource` (crew report badge "From the app"). Crew may backdate up to 2 h
  (`Trip.CrewBackdateWindow`) and undo their own latest tap for 10 min. Confirm Pick Up / Delivery on a
  trip load now records the stop arrival, through the same sheet (`TripEventRecorder`, shared by
  Today and Load Details). The sheet offers the GPS zone-entry time (`LoadProximityWatcher.arrivalSeenAt`,
  in memory only), never earlier than the truck's last departure. One stop at a time: a visit can't
  overlap another stop's recorded times (`Trip.EnsureNoOverlap`, office and crew, corrections too), and
  the crew must leave a stop before arriving elsewhere (Today hides the other Arrived buttons). A
  completed trip stays on Today until its garage arrival, up to 12 h. Not yet: offline queue
- ⚠️ **`Load.IsInProximity` is to the next checkpoint only** (origin while Dispatched, destination once
  PickedUp): `Load.UpdateStatus` clears it on any status change, and the app's `LoadProximityWatcher`
  checks only that checkpoint. To walk a trip from a desk, move the emulator
  (`adb emu geo fix <lng> <lat>` or Extended Controls → Location) - there is no in-app shortcut
- ⚠️ **Driver endpoints act on the caller's own loads only**: `Modules/Operations/Loads/DriverLoadAccess`
  (the driver of a live trip carrying the load, a rescue driver holding one of its stops, the truck's
  main driver for a load on no trip, or anyone with `Load.Manage`) guards `confirm-load-status`,
  `update-load-proximity`, `documents/pod` and `documents/bol`; a POD / BOL `TripStopId` must be the
  load's own stop, and `device-token` takes only the caller's own id. Reuse it on every new driver
  endpoint. `GET /loads?userId=` also matches trips the user drives or rescues (Past Loads, the
  proximity watcher)
- **Driver push follows the trip, not the truck**: `Trips/Events/TripDriverPushHandler` pushes
  "Aug 29: Superb Catch, Inc., 4 drops" to `Trip.Driver` on dispatch, a driver change on a dispatched
  trip (`TripDriverChangedEvent`, both drivers) and cancelling a dispatched / in-transit trip
  (`TripCancelledEvent`). The old load-assigned push skips loads on a trip, and the trip-assigned
  push skips draft trips
- **Crew report (office trip entry)**: `tms-portal/pages/trips/trip-report/` (`/trips/:id/report`,
  the clipboard icon on each trips-list row, "Crew report" on trip details). One page laid out like
  the driver's Messenger report; times typed as `2329` / `23:29` / `2329H` and dated by
  `utils/report-times.ts` (a time earlier than the one before it is the next day). Saves through
  `RecordTripReport` (`POST /trips/{id}/report`): stops in trip order then garage times, one
  `SaveChanges`, all-or-nothing, errors prefixed "Warehouse:" / "Drop n:" / "Garage:". Already
  recorded events are skipped and show read-only; changing them is a correction (below).
  `utils/same-visit.ts` groups a warehouse's pickups the way `Trip.GetSameVisitPickups` does
- **Corrections (changing a recorded event)**: recording stays once-only; a correction is its own
  action with a required reason. `Trip.Corrections.cs` (`CorrectStop` - a pickup corrects its whole
  warehouse visit - and `CorrectGarageTimes`) runs the recording rules again and returns one
  append-only `TripCorrection` row per changed value (field, old -> new as text, reason, who,
  when); the handlers add them through `TripCorrectionWriter` (no navigation, so `AddAsync` is the
  only way they save). A correction never records an event for the first time or clears one, and
  is refused on draft or cancelled trips. Times within the same minute count as unchanged (the
  page shows minutes only). The stop's own `ArrivalRecordedBy/At` keeps the first entry.
  Commands `CorrectTripStop` / `CorrectTripGarageTimes`, query `GetTripCorrections`
  (`POST trips/{id}/stops/{stopId}/correction`, `POST trips/{id}/garage-times/correction`,
  `GET trips/{id}/corrections`, `Load.Manage` / `Load.View`). UI: a Correct button per recorded
  block on the crew report (`components/stop-correction-dialog`, `garage-correction-dialog`), the
  history under each block (`utils/corrections.ts`)
- ⚠️ **A corrected outcome doesn't re-price the trip.** When a drop flips between delivered and
  foul trip / cancelled by client, `Trip.PriceOutdatedSince` is stamped; `IsPriceOutdated`
  (stamp later than `RatedAt`) shows "Price out of date" on the rate card until it is re-priced.
  Approval of corrections (who may correct, open question 6) is not built
- **Crew report photos** (2026-10-06): a photo strip on each report block
  (`components/report-stop-photos`, page-scoped `store/trip-photos.store.ts`) - paste (Ctrl+V on the
  strip), drop or browse; JPEG/PNG/WebP, 10 MB each. A warehouse photo is `PickupInspection`, a drop
  photo `ProofOfDelivery`, stored as `DeliveryDocument`s with `TripStopId`. One blob per photo with a
  document per stop it covers, so every load of a warehouse visit shows it; photos are grouped back by
  `BlobPath` (`TripStopPhotoMapper`), and `DeleteDocument` keeps a blob another document still uses.
  `GET/POST trips/{id}/photos`, `DELETE trips/{id}/photos/{photoId}` (`GetTripStopPhotos`,
  `AddTripStopPhotos`, `DeleteTripStopPhoto`). They also list on the load's POD / BOL tab
- **Changing the truck, crew or drops from the crew report** (2026-10-05): "Change" on the report
  header (`components/report-header`, `report-header-editor`; Enter moves on, Ctrl+S saves, Esc
  closes) and "Remove drop" on an unvisited drop block, both through `UpdateTrip` (`utils/trip-header.ts`
  always sends the current crew - the API reads an omitted driver/helper as "no one"). Pickers offer
  crew available on the trip's day (earliest requested pickup, as `TripDay`), not today. Refused while
  the report has unsaved times, since a change reloads the page. Adding a drop stays on Edit Trip
- ⚠️ **The header locks at the first recorded arrival**, not at a status: `Trip.HasRecordedStops`
  guards `AssignToTruck` and `AssignCrew` (a drop's arrival without the warehouse leaves the trip
  Dispatched), and `RemoveLoad` refuses a load whose pickup or drop has an arrival. `UpdateTrip` also
  refuses removing every drop. Removing a drop deletes its load and, by cascade, the load's draft
  invoice; a load whose invoice has a payment would fail at the database (payments FK is NO ACTION)
- **Rescue** (2026-10-08): another truck and crew taking over drops after a breakdown or accident.
  `Trip.Rescues.cs` - `RecordRescue(RescueSpec)` / `UndoRescue`; `TripRescue` (truck, driver,
  helpers, `TransferredAt`, place, reason, optional `IncidentId`) and `TripStop.RescueId` on each
  drop it took. The trip keeps its truck, booking and price (one truck-trip billed); only loaded,
  unvisited drops can be taken; a rescued drop's arrival can't be before the transfer; the trip's own
  garage times bracket only its own stops (`OwnStops`). Undo until a rescued drop is reached.
  `POST/DELETE trips/{id}/rescues` (`Load.Manage`); `TripDto.Rescues` on the single-trip read. UI: the
  "Incidents and rescues" card on the crew report (`components/report-rescues`, `rescue-dialog`,
  `utils/rescue.ts`) and a "Rescue · truck" badge on each rescued drop. The rescue truck's own garage
  times: `Trip.RecordRescueGarageTimes` (left before the transfer, back after its last rescued drop),
  `PUT trips/{id}/rescues/{rescueId}/garage-times`, `rescue-garage-dialog`. Not yet: the crew plan
  doesn't show the rescue truck as used that day
- **Price out of date** is also stamped (only on a priced trip) when a drop is added or removed or
  the truck's tonnage class changes - pricing reads both; a same-size truck swap leaves the price

### Crew plan (daily crew assignment)

The dispatcher's morning plan, replacing Monday's CCL OPS - DISPATCH rows: per truck per day, the
unit's status (`TruckDayStatus`) and its driver plus up to two helpers.

- Domain: `Entities/CrewPlan/CrewAssignment.cs` - unique per (date, truck); `Date` is the tenant's
  local day (`TenantSettings.LocalDate`)
- Application: `Modules/Operations/CrewPlan/` - `SetCrewAssignment` (upsert per truck-day),
  `DeleteCrewAssignment`, `CopyCrewPlan` (fills only unplanned trucks, drops people already busy),
  `GetCrewPlan` (a row per active truck, planned or not); `CrewPlanService` guards one person on two
  trucks the same day
- ⚠️ **New trips take their crew from it.** `CreateTripHandler`: when the request names no crew,
  the truck's plan for the trip's day (earliest requested pickup, else today) supplies it, then the
  truck's main driver. The trip wizard pre-fills the same from today's plan when a truck is picked.
  Other paths that call `Trip.Create` directly (AI dispatch) don't read the plan yet
- Not in a truck merge's `MergeReferences`: the (date, truck) unique index would collide
- **Per-person daily status**: `Entities/CrewPlan/CrewStatusEntry.cs` - one row per person per day
  (a driver `EmployeeId` or a `HelperId`, check-constrained to exactly one), `CrewDayStatus`
  (available / reserve / absent / on leave / suspended / training / lent out). Sparse: no row means
  available, and "available" with no notes deletes the row. `SetCrewStatus` takes a date range
  (leave, suspension) and takes anyone absent / on leave / suspended off that day's plan rows (the
  unit turns `Understaffed` if it was `Available`); `GetCrewDay` lists every active driver + helper
  with status and truck. `CrewDayStatusExtensions.Blocking` / `.Offered` are the two lists every
  check filters on
- ⚠️ **Who can be picked**: the plan save, `CreateTrip` and `UpdateTrip` refuse blocking statuses
  (`ICrewPlanService.FindUnavailableAsync`, on the trip's day from `TripDay`); "Copy previous day"
  leaves them off. The pickers pass `AvailableOn` to `GET /employees` / `GET /master-data/helpers`,
  which then return active people with an offered status only - with `SKIP_CACHE`, because a
  `/crew-assignments` write changes the answer
- Drivers (`Employee`) and helpers carry `EmploymentType` (regular / on-call) and `HomeUnit`
  (`CrewUnit`: Cold Chain / wing van / trucking); `CrewSeeder` sets them and fills them in on
  records seeded before they existed
- API/UI: `CrewAssignmentController.cs` (`/crew-assignments`, `/crew-assignments/crew`,
  `/crew-assignments/crew/status`, `Permission.Load.*`), `tms-portal/pages/crew-plan/` (Dispatch >
  Crew Plan: Trucks, Crew and Bookings-without-a-trip tabs; the last is `unassigned-bookings-panel`,
  see _Booking_ under Freight Forwarding)

### Trucks

- Domain: `Entities/Truck.cs`
- Application: `Modules/Operations/Trucks/Commands/`, `Modules/Operations/Trucks/Queries/`
- API/UI: `TruckController.cs`, `tms-portal/pages/trucks/`

### Customers

- Domain: `Entities/Customer.cs`
- Application: `Modules/IdentityAccess/Customers/Commands/`, `Modules/IdentityAccess/Customers/Queries/`
- API/UI: `CustomerController.cs`, `tms-portal/pages/customers/`

### Customer users

- Domain: `Entities/Customer/CustomerUser.cs`
- Application: `Modules/IdentityAccess/CustomerUsers/Commands/`
- API/UI: `CustomerUserController.cs`, `customer-portal/pages/`

### Containers (ISO 6346)

- Domain: `Entities/Container/` - `Number` is canonicalised on write by `Container.NormalizeNumber`
- Application: `Modules/Operations/Containers/Commands/`, `Modules/Operations/Containers/Queries/`
- API/UI: `ContainerController.cs`, `tms-portal/pages/containers/`
- Feature flag: `TenantFeature.IntermodalContainers` (Professional+; off by default)

### Terminals (UN/LOCODE)

- Domain: `Entities/Terminal/` - `Code` is canonicalised on write by `Terminal.NormalizeCode`
- Application: `Modules/Operations/Terminals/Commands/`, `Modules/Operations/Terminals/Queries/`
- API/UI: `tms-portal/pages/terminals/`
- Feature flag: `TenantFeature.IntermodalContainers` (shared with Containers)

### Employees / Drivers

- Domain: `Entities/Employee.cs`
- Application: `Modules/IdentityAccess/Employees/Commands/`, `Modules/IdentityAccess/Employees/Queries/`
- API/UI: `EmployeeController.cs`, `DriverController.cs`, `tms-portal/pages/employees/`
- Set password: `Commands/SetEmployeePassword/` (`PUT /employees/{userId}/password`, `Permission.Employee.Manage`; same tenant only, never self/platform admins, Owner only by Super Admin/Admin), `employees/components/set-password-dialog/`. Exists because drivers sit on placeholder `@crew.skybest.local` emails and prod seeds them passwordless

### Time entries

- Domain: `Entities/TimeEntry.cs`
- Application: `Modules/Operations/TimeEntries/Commands/`, `Modules/Operations/TimeEntries/Queries/`
- API/UI: `TimeEntryController.cs`, `tms-portal/pages/timesheets/`

### Dashboard / stats

- Application: `Modules/Platform/Stats/Queries/`, `Modules/Platform/Reports/Queries/`
- API/UI: `StatController.cs`, `ReportController.cs`, `tms-portal/pages/home/` (+ its `components/` panels), `pages/reports/`. `pages/dashboard/` is a redirect-only stub kept for old bookmarks - don't add anything there

### Onboarding

- Application: `Modules/Platform/Onboarding/Queries/GetOnboardingProgress/` - read-only checklist; step keys are camelCase, labels/icons/routes live on the client. Each step declares a `TenantFeature`, so a disabled feature drops its step rather than shipping one that can never complete
- API/UI: `OnboardingController.cs` (`GET /onboarding/progress`, `Permission.Employee.Manage` - Owner + Manager), `tms-portal/pages/home/components/onboarding-checklist/`

### Solo / owner-operator mode

- Domain: `Primitives/Enums/Tenant/OperatingMode.cs` (`Fleet` | `SoloOperator`), stored on the `TenantSettings` VO (`settings_operating_mode` on `tenants`, master DB)
- Application: `Modules/IdentityAccess/Tenants/Commands/CreateTenant` + `UpdateTenant` carry it; `Modules/Platform/Onboarding/` drops the `inviteTeam` step in solo mode
- Infrastructure: `Infrastructure.AI/Agents/Dispatch/AIDispatchSystemPrompt.cs` - solo swaps in a `## Fleet Profile: SOLO OWNER-OPERATOR` section
- API/UI: `tms-portal/core/services/tenant.service.ts` (`isSoloMode`), `core/services/sidebar-nav.service.ts` (`SOLO_HIDDEN_ITEMS`), `tms-portal/pages/settings/company-settings/`, `admin-portal/shared/components/tenant-form/`
- Note: solo relies on Owner holding `Permission.Driver.*` (`Shared.Identity/Policies/TenantRolePermissions.cs`), and driver-facing queries keying off `Truck.MainDriverId`/`SecondaryDriverId` rather than the role. There is no multi-role model
- Seed: `DbMigrator` tenant `solo` (`SeedData/solo.json`, `SeedDataKey`/`OperatingMode`/`DataScale` on `Models/DemoTenantConfig.cs`)

## AI dispatch

### Dispatch conversations & turns

Tenant-shared, not per-user: any user with `Permission.Dispatch.View` can list/read a conversation;
`Permission.Dispatch.Manage` sends/cancels/renames/deletes. Handlers do not check ownership -
`CreatedById` is audit-only. Shares the conversation/turn machinery with the
[AI copilot](../docs/ai-copilot.md) (`AgentConversation`/`AgentMessage`, `AgentTurnService`,
`AgentTranscriptReplay`), scoped by `AgentConversationKind.Dispatch`; each turn is an `AgentSession`
with `Type == Dispatch`.

- Domain: `Entities/AIDispatch/AgentSession.cs`, `AgentConversation.cs`, `AgentMessage.cs`
- Application: `Modules/Integrations/AIDispatch/Commands/` (`Create`/`Rename`/`Delete`AIDispatchConversation, `SendAIDispatchMessage` - concurrency guard + quota gate, mirrors `SendAICopilotMessage`; `CancelAIDispatchTurn`), `Modules/Integrations/AIDispatch/Queries/` (`GetAIDispatchConversations` - no owner filter, `GetAIDispatchConversationById` - messages + decisions + per-turn `AgentSessionDto` stats)
- Infrastructure: `Infrastructure.AI/Agents/Dispatch/AIDispatchService.cs` (thin adapter onto the shared `AgentTurnService`) + `DispatchAgentSurface.cs` (`IAgentSurface`: no per-caller tool scoping, tenant-wide broadcasts, injects a fresh fleet-state snapshot into only the current turn's final user message - never persisted) + `AIDispatchConversationBuilder.cs` (system prompt, transcript replay, snapshot injection); `Infrastructure.Communications/SignalR/Hubs/AIDispatchHub.cs` (tenant-wide `dispatch-board:{tenantId}` group, mounted at `/hubs/ai-dispatch`)
- Jobs: `Trackwise.API/Jobs/AIDispatchTurnJob.cs` (re-checks `AgenticDispatch` inside the job, mirrors `AICopilotTurnJob`)
- API/UI: `AIDispatchController.cs` (`ai/dispatch/conversations` CRUD + messages + cancel, mirroring `AICopilotController`'s route style), `tms-portal/pages/ai-dispatch/`

### Dispatch decisions

- Domain: `Entities/AIDispatch/AgentDecision.cs`
- Application: `Modules/Integrations/AIDispatch/Commands/Approve*`, `Reject*` - append a System note to the decision's conversation transcript and broadcast it tenant-wide, but only when the session has a `ConversationId` (old sessions may predate conversations)
- Infrastructure: `Infrastructure.AI/Agents/AgentDecisionProcessor.cs`
- API/UI: (under `ai-dispatch/`)

### Dispatch policy (learned preferences)

- Domain: `Entities/AIDispatch/AIDispatchPolicy.cs` (one row per tenant; `GeneratedContent` is job-owned, `ManualContent` is dispatcher-owned)
- Application: `Modules/Integrations/AIDispatch/Services/AIDispatchPolicyLearner.cs` + `DecisionHistoryDigest.cs` + `AIDispatchPolicyPrompt.cs`; `Commands/{Update,Regenerate,Delete}AIDispatchPolicy/`, `Queries/GetAIDispatchPolicy/`
- Infrastructure: injected into the prompt by `Infrastructure.AI/Agents/Dispatch/AIDispatchSystemPrompt.cs` (`LearnedDispatchPolicy`), loaded in `AIDispatchConversationBuilder`
- Jobs: `Trackwise.API/Jobs/AIDispatchPolicyLearningJob.cs` (nightly, `Cron.Daily(4)`)
- API/UI: `AIDispatchController` `ai/dispatch/policy`, `tms-portal/pages/ai-dispatch/dispatch-policy/`

### Tool registry

- Infrastructure: `Infrastructure.AI/Tools/` (catalogue, schema/binding) + `Tools/{Dispatch,Financial,Operations,LoadBoard,Intermodal}/`
- API/UI: shared with `Trackwise.McpServer` and the AI copilot
- Behavior metadata (`IsWrite`, `RequiredPermission`, `DecisionType`, `RequiredFeature`) lives on
  each `AgentToolDefinition` - there is no separate write-tool list anywhere
- Intermodal reads: `Tools/GetContainerStatusTool.cs` (ISO 6346), `Tools/GetTerminalInfoTool.cs` (UN/LOCODE).
  Gated by `TenantFeature.IntermodalContainers` - the schemas, the prompt section and the tools' own
  guard all move together; MCP lists every tool, so the guard inside each tool is the real gate.

### Broker rate negotiation (email)

See [docs/broker-email-negotiation.md](../docs/broker-email-negotiation.md). Gated by
`TenantFeature.AIRateNegotiation` + `Permission.Negotiation.View/Manage`. Every outbound offer is an
approved `AgentDecision`; inbound mail can only append a message and ask for a turn.

- Domain: `Entities/Negotiation/RateNegotiation.cs` (owns the reply token and the round cap), `NegotiationMessage.cs`, `LaneRateFloor.cs`; `Entities/InboundEmailRoute.cs` (**master** DB - maps a reply token to a tenant); `AgentDecision.NegotiationId`, `TenantSettings.DefaultRateFloorPerMile`
- Application: `Modules/Integrations/Negotiation/` - `Services/` (`LaneRateFloorResolver`, `NegotiationEmailComposer` - the one owner of broker-facing formatting and sanitization, `NegotiationTurnStarter`, `EmailReplyParser`), `Commands/` (`ProposeCounterOffer` - floor gate then credit gate then send-before-persist; `CloseNegotiation`; `ProcessInboundNegotiationEmail`; lane-floor CRUD), `Queries/` (`GetNegotiations`, `GetNegotiationById`, `PreviewCounterOffer`, `GetRateFloorContext`, `GetLaneRateFloors`); `Modules/Integrations/Webhooks/Commands/ProcessResendWebhook/`; shared credit gate in `Modules/Integrations/LoadBoard/Services/BrokerCreditGate.cs`
- Infrastructure: `Infrastructure.Communications/Email/` (`ResendThreadedEmailSender` - reply-to plus `In-Reply-To`/`References`, `ResendInboundEmailReader` - the Received Emails API, `ResendWebhookVerifier`, `Templates/BrokerCounterOffer.liquid`); `Infrastructure.Integrations.Common/WebhookSignature.VerifySvix`; `Infrastructure.AI/Tools/Negotiation/` (`get_rate_floor`, `get_negotiation_thread`, `propose_counter_offer`)
- Jobs: `Trackwise.API/Jobs/NegotiationExpirySweepJob.cs` (every 6 hours - expires lapsed threads and revokes their routes), `NegotiationWakeJob.cs` (delayed retry when a reply lands mid-turn)
- API/UI: `NegotiationController.cs` (`negotiations` - list, detail, close, decision preview; sending is deliberately absent), `RateFloorController.cs` (`ratefloors`), `POST /webhooks/resend`, `tms-portal/pages/ai-dispatch/negotiations/` + `negotiation-details/` + `rate-floors/`
- Demo data: `DbMigrator/Seeders/FakeData/NegotiationSeeder.cs` - lane floors, `Demo` load board listings and one thread per status per demo tenant; the tenant-wide fallback floor comes from `DemoTenantsSeeder`

## AI copilot

See [docs/ai-copilot.md](../docs/ai-copilot.md). Conversational agent sharing the dispatch tool
registry, `AgentLoopRunner`, decisions, and quota. Gated by `TenantFeature.AICopilot` +
`Permission.Copilot.View/Manage`; tool catalogue scoped per user.

### Conversations & turns

- Domain: `Entities/AIDispatch/AgentConversation.cs`, `AgentMessage.cs` (transcript with tool_use ids; `Kind` distinguishes the per-user Copilot conversations here from the tenant-shared Dispatch ones above - every copilot query/list/get filters `Kind == Copilot`); `AgentSession.Type == Copilot` per turn
- Application: `Modules/Integrations/AICopilot/Commands/` (send returns the server-stamped `UserMessageCreatedAt` so the client's optimistic echo never sorts by a browser clock; rename via `RenameAICopilotConversation`), `Queries/` (incl. `GetAICopilotQuotaStatus` - the shared AI quota behind `Permission.Copilot.View` for tenants without AgenticDispatch)
- Infrastructure: `Infrastructure.AI/Agents/` (`AgentTurnService.cs` - the shared turn lifecycle every conversational agent runs through, parameterized by `IAgentSurface`; `AgentTranscriptCodec.cs`, `AgentTranscriptReplay.cs`) and `Agents/Copilot/` (`AICopilotService.cs` - thin adapter onto `AgentTurnService` + `CopilotAgentSurface.cs`, `AICopilotConversationBuilder.cs`, `AICopilotSystemPrompt.cs` - dispatch-action guardrails render only when the caller's tool catalogue includes the dispatch write tools); `Infrastructure.Communications/SignalR/Hubs/CopilotHub.cs` (`/hubs/copilot`, authorized, per-user groups)
- Jobs: `Trackwise.API/Jobs/AICopilotTurnJob.cs`
- API/UI: `AICopilotController.cs` (`ai/copilot` - conversations CRUD + rename, messages, cancel, `quota`, decisions approve/reject), `tms-portal/shared/layout/copilot-drawer/`; state in `core/store/copilot.store.ts` (persists drawer width, polls stuck Running turns, gates the launcher on `Copilot.View`)

### Copilot tools (loads, customers, invoicing, expenses, maintenance)

- Infrastructure: `Tools/SearchLoadsTool.cs`, `GetLoadTool.cs`, `SearchCustomersTool.cs`, `GetInvoicesTool.cs`, `GetInvoiceTool.cs`, `SearchExpensesTool.cs`, `GetExpenseStatsTool.cs`, `GetUpcomingMaintenanceTool.cs`, `CreateLoadInvoiceTool.cs`, `SendInvoiceTool.cs`, `CreatePaymentLinkTool.cs` (+ shared `AgentToolJson.cs` schema and binding)

### LLM providers

- Infrastructure: `Infrastructure.AI/Llm/` (`LlmProviderFactory`) and `Llm/Providers/` (Anthropic, OpenAI)

### Quota / pricing

- Domain: `Entities/Subscription/SubscriptionPlan.cs` (`WeeklyAIBudgetUsd`, required; unsubscribed tenants fall back to the Enterprise budget via `Application.Abstractions/AIDispatch/AIWeeklyBudget.cs`)
- Application: `Application.Abstractions/AIDispatch/IAIQuotaService.cs` (port)
- Infrastructure: `Infrastructure.Persistence/Services/AIDispatch/AIQuotaService.cs` (cost-based quota tracking), `Infrastructure.AI/Llm/LlmPricing.cs` (per-token pricing), `Application.Abstractions/Payments/Stripe/AIOverageBilling.cs` (cost → Stripe metered units; shared by Stripe metering and the tenant-visible accrued-charges figure)
- API/UI: (quota bar in `tms-portal/pages/ai-dispatch/`), `admin-portal/pages/ai-settings/tenant-quotas/` (per-tenant usage + revenue-vs-LLM-cost margin report)
- Quota numbers live in `SubscriptionPlanSeeder` + admin portal only - never exposed to tenants (tenant API returns a percentage)

### Global AI settings

- Application: `Modules/Platform/AISettings/` (`GetAISettings`, `UpdateAISettings`); `Abstractions/AIDispatch/LlmModelCatalog.cs`
- Infrastructure: `SystemSettingsService` (`AI.Model`/`AI.ReasoningEffort` keys)
- API/UI: `AISettingsController.cs`, `admin-portal/pages/ai-settings/`

### Background runner

- API/UI: `Jobs/AIDispatchTurnJob.cs`

### MCP server

- API/UI: `Trackwise.McpServer/` (`McpToolSurface` over `IAgentToolRegistry`)

## Compliance & safety

### ELD / HOS logs

- Domain: `Entities/Eld/HosLog.cs`, `HosViolation.cs` (`RuleSetCode`), `DriverHosStatus.cs`, `ValueObjects/HosLimits.cs`
- Application: `Modules/Compliance/Eld/Commands/`, `Modules/Compliance/Eld/Queries/GetHosLimits/`, `Modules/Compliance/Eld/Services/RuleSetSelector.cs` (region → FMCSA / EU 561/2006)
- Infrastructure: `Infrastructure.Integrations.Eld/` (Samsara, Motive, TT ELD, Geotab, Demo); local `Common/EldJsonOptions.cs` (per-provider camel/snake casing), plus `TryGetFromJsonAsync` from `Integrations.Common`
- API/UI: `EldController.cs` (incl. `/eld/rules/limits`), `tms-portal/pages/eld/` (`EldRulesService`, region-aware dashboard)

### ELD provider config

- Domain: `Entities/Eld/EldProviderConfiguration.cs`
- Infrastructure: `Infrastructure.Integrations.Eld/` factory
- API/UI: (under `eld/`)

### ELD sync

- Application: `Modules/Compliance/Eld/Commands/ProcessEldWebhook/` (stamps `RuleSetCode` from tenant region)
- Infrastructure: `WebhookSignature.VerifyHmacSha256` (used by Geotab)
- API/UI: `Jobs/EldSyncJob.cs`, webhooks: `/webhooks/eld/{samsara,motive,geotab,tteld}`

### DVIR inspections

- Domain: `Entities/Safety/DvirReport.cs`, `DvirDefect.cs`
- Application: `Modules/Compliance/Dvir/Commands/`, `Modules/Compliance/Dvir/Queries/`
- API/UI: `DvirController.cs`, `tms-portal/pages/safety/`
- Feature flag: `TenantFeature.Dvir` (Starter+; enabled by default; gates commands and queries - separate from `Safety`, which stays Professional)

### Vehicle inspections

- Domain: `Entities/Inspection/VehicleConditionReport.cs`
- Application: `Modules/Compliance/Inspections/Commands/`, `Modules/Compliance/Inspections/Queries/`
- API/UI: `InspectionController.cs` (list/get/create + `GET /inspections/parts`), `VinController.cs` (`GET /vins/{vin}`)

### Incidents (accidents, breakdowns, damaged or missing goods)

One record for everything that went wrong on the job, replacing the numbered cases Cold Chain kept in
Monday's WEEKLY HIGHLIGHTS (`CCL-CASENO.###`) and the US-style accident report it grew out of
(table `accident_reports` -> `incidents`, migration `AddIncidentsAndTripRescues`, old statuses folded;
`IncidentCaseNumbersAttachmentsRescueGarage` turned the identity number into `CaseNumber`).

- Domain: `Entities/Safety/Incident.cs` (+ `.Status.cs`: Open -> Under review -> Resolved -> Closed;
  resolving needs the corrective action, a resolved one reopens with a reason, closed is final; only an
  open, never-reviewed one can be deleted), `IncidentDetails` / `IncidentParties` (the edit shape),
  `IncidentKind` (accident, breakdown, backload, delay, cancellation, no truck or crew, product
  damage, loading error, wrong drop, documents, temperature, traffic violation, theft, other),
  `IncidentWitness`, `IncidentThirdParty`. Trip, client, truck, driver and helpers are all optional
  FKs (SetNull). PH fields: police station, barangay blotter no., paid to others, charged to crew +
  note, `Prevention` (the weekly report's improvement / future plans)
- Case numbers: `CaseNumber` from the tenant's `Incident` number series (`IMasterDataCodeGenerator`,
  default `CASE-001`); ph's series is `CCL-CASENO.###` carrying on from Monday's last case
- Attachments: `IncidentDocument` (documents table, owner type Incident, cascade) - photos and the
  police report / blotter as PDF. `POST incidents/{id}/attachments` (multipart), `DELETE
incidents/{id}/attachments/{attachmentId}`; viewed through the generic document download. UI
  `components/incident-attachments` (paste / drop / browse, lightbox)
- `MaintenanceRecord.IncidentId` links the repairs it caused (`SetIncidentRepairs`, same truck, one
  incident per repair); the total cost is linked repairs + paid to others, never typed
- Application: `Modules/Compliance/Incidents/` (`IncidentFields` + shared validator, `IncidentEdit` -
  load/change/save/map, `IncidentReader` - names batched, repairs and rescues on the details read,
  `IncidentPartiesResolver` - client from the trip's drops when not given)
- API/UI: `IncidentController.cs` (`/incidents`, `Safety.View` / `.Manage`, **no** `safety` feature
  gate), `tms-portal/pages/incidents/` (list, add - `?tripId=` prefills from the trip, edit,
  details with review/resolve/reopen/close, link-repairs dialog); nav Fleet & Compliance > Incidents.
  The safety report and the Safety dashboard read accidents as incidents of kind Accident
- Seeds: `MondayIncidentSeeder` (ph, prod too) imports Monday's weekly-report cases from
  `SeedData/ph-incidents.json` (62 of 80 numbered cases - achievements like done collections and
  client meetings are left out - plus 2 Monday never numbered), generated by
  `monday-export/scripts/monday_incidents.py`; it sets the series past the highest case. Severity is
  judged from the text (Monday has none); dates are noon Manila. Fake `IncidentSeeder` (US accidents)
  only where there is no Monday data

### Driver behavior

- Domain: `Entities/Safety/DriverBehaviorEvent.cs`
- Application: `Modules/Compliance/Safety/Commands/`, `Modules/Compliance/Safety/Services/`
- API/UI: `DriverBehaviorController.cs`

### Maintenance (repairs, PMS, schedules)

Every service or repair on a truck, replacing Cold Chain's Monday REPAIR EXPENSES and REEFER SYSTEM
PMS boards. Repairs are paid through nSFS requests for payment (`RFP-RFV`, "RFV26-001517-M"), so a
record carries that **requisition number**; one requisition often covers several trucks, each its
own record, and `/maintenance/requisitions/{number}` totals them to check against nSFS. Approval
stays in nSFS e-approval - none is built here.

- Domain: `Entities/Maintenance/` - `MaintenanceRecord` (`RequisitionNumber`, `VehicleSystem` - which
  part of the truck, `Reefer` for the fridge unit; `ReplaceParts` sets parts cost to the lines' sum,
  or the typed amount when there are no lines), `MaintenancePart` (+ `Description`),
  `MaintenanceSchedule` (`SetInterval`, `RecordService`, `DaysUntilDue`). Types `Repair` and
  `ReeferPms` added to `MaintenanceType`
- ⚠️ **A logged service moves its schedule on by itself**: given no schedule, it links the truck's
  one active schedule of the same type (`MaintenanceRecordWriter`), which is why a truck keeps one
  active schedule per type. A service older than the last one never sets the schedule back.
  Overdue means past the due **day**; a time schedule with no service on record is due today
- Application: `Modules/Operations/Maintenance/` - record and schedule create/update/delete
  (shared-fields validators), `GetMaintenanceRecords` (search, type, system, requisition filters),
  `GetMaintenanceRequisition`, `GetMaintenanceSchedules`, `GetUpcomingMaintenance`;
  `Compliance/Safety/Services/MaintenanceReminderService` (due-day notices; overdue ones weekly)
- API/UI: `MaintenanceController.cs` (`/maintenance/records|schedules|requisitions|upcoming`),
  `tms-portal/pages/maintenance/` (records, record form with part lines, requisition page, upcoming),
  the truck page's Maintenance tab `shared/components/fleet/truck-maintenance` +
  `maintenance-schedule-dialog`, `Jobs/MaintenanceReminderJob.cs`
- Seed (`ph`): `RepairHistorySeeder` (359 records on 283 nSFS requisitions, ₱3.42M, Jan 2025 - Sep
  2026; skips a truck that already has a record on the same requisition) and `ReeferPmsSeeder`
  (weekly PMS per active reefer) from `SeedData/ph-repairs.json`, built by
  `Documents\monday-export\scripts\nsfs_repairs.py` (read-only from live nSFS; the PMS part still
  comes from the Monday board via `repairs.py`); the fake `MaintenanceSeeder` skips a tenant with
  real repairs. Migration `AddRepairRequisitions`
- Not yet: genset maintenance (records are truck-only), the older "REPAIR EXPENSES MONTH OF FEB"
  board (no dates or requisitions)

### Driver licensing

- Domain: `Entities/Employee/DriverLicense.cs`
- Application: `Modules/IdentityAccess/Employees/Commands/{Create,Update,Delete}DriverLicense/`, `Application.Abstractions/Dispatch/IDispatchEligibilityService`, `Modules/Compliance/Safety/Services/LicenseExpiryReminderService`
- Infrastructure: `Infrastructure.AI/Tools/Dispatch/CheckDispatchEligibilityTool.cs`
- API/UI: `EmployeeController` (extended), `tms-portal/pages/employees/components/driver-licenses-tab/`, mobile `screens/MyLicensesScreen.kt`, `Jobs/LicenseExpiryReminderJob.cs`

### ADR / Hazmat

- Domain: `Truck.AdrEquipment`, `Load.HazmatClass`, `ValueObjects/AdrEquipment.cs`
- Application: `IDispatchEligibilityService` (gates `DispatchLoad`, `BulkDispatchLoads`, `DispatchTrip` handlers)
- API/UI: (under Loads / Trucks pages - hazmat fields on load-form, ADR fieldset on truck-form)

### IFTA reporting

- Domain: `Entities/Ifta/` (`TruckLocationHistory`, `TruckJurisdictionMileage`, `IftaQuarterSnapshot`), `Entities/IftaTaxRate.cs` (master)
- Application: `Modules/Compliance/Ifta/` (`TruckLocationRecorder`, `IftaReportService`, quarter-close + filing-reminder services, `IftaQuarters`), `Modules/Compliance/Ifta/TaxRates/` (admin rate CRUD), `Modules/Platform/Reports/Queries/Ifta/`
- Infrastructure: `Infrastructure.Routing/Geospatial/JurisdictionResolver.cs` (NTS + embedded boundaries, built once at startup by `JurisdictionResolverWarmup`); `Infrastructure.Documents/Pdf/Ifta/`
- **Traps**: closed quarters are served verbatim from `IftaQuarterSnapshot.ReportJson` (sole source of truth, deliberately not denormalized into columns). Tax-rate `(Year, Quarter, Jurisdiction)` uniqueness is enforced ONLY by `IftaTaxRateUniqueness.FindConflictAsync` - complex-type members can't be in a DB unique index, so every write path must probe or a duplicate silently changes computed tax due
- API/UI: `ReportController` `/reports/ifta` + `/reports/ifta/pdf`, `IftaTaxRateController.cs` `/ifta/rates`, `Jobs/IftaQuarterCloseJob.cs`, `Jobs/IftaFilingReminderJob.cs`, `tms-portal/pages/reports/ifta-report/`, `admin-portal/pages/ifta-rates/`

### Privacy (GDPR)

- Domain: `Entities/Privacy/`
- Application: `Modules/Compliance/Privacy/` (Commands, Queries, Services)
- Infrastructure: `Infrastructure.Documents/Privacy/`, `Infrastructure.Persistence/Privacy/DataAnonymizer.cs`
- API/UI: `PrivacyController.cs`, `Jobs/Data{Export,Deletion,Retention,ExportExpiry}Job.cs`, `<ui-cookie-banner>`, `admin-portal/data-requests/`

## Financial

### Invoices (TPH)

- Domain: `Entities/Invoice/Invoice.cs` + `LoadInvoice`, `PayrollInvoice`, `SubscriptionInvoice`, `BookingInvoice`, `Invoice.RecalculateTotals`, `InvoiceTaxLine`
- Application: `Modules/Financial/Invoices/Commands/`, `Modules/Financial/Invoices/Queries/`, `IInvoiceTaxApplier`
- API/UI: `InvoiceController.cs` + `preview-tax`, `tms-portal/pages/invoices/`, `customer-portal/pages/invoices/`, `tms-portal/pages/forwarding/billing/`
- Booking billing: draft invoices reconcile Booking charges; approved invoices are immutable; later charges use linked Supplemental invoices. Pre-approval cancellation retains the invoice and releases its charges for an explicit linked Replacement. See [docs/invoices.md](../docs/invoices.md).

### Tax engine

- Domain: `Entities/TenantTaxRate.cs`, `Invoice.TaxBehavior`, `Invoice.TaxBreakdownJson`, `ValueObjects/TaxJurisdiction.cs` - build jurisdictions with `TaxJurisdiction.Create(country, region)` on every write path; it upper-cases and normalizes a blank region to null, which is what the equality comparisons rely on
- Application: `Application.Abstractions/Tax/ITaxCalculator`, `Modules/Financial/Tax/Services/IInvoiceTaxApplier`, `Modules/Financial/Tax/Commands/`, `Modules/Financial/Tax/Queries/`, `Modules/Financial/Invoices/Queries/PreviewInvoiceTax/`
- Infrastructure: `Infrastructure.Tax/` (`StripeTaxCalculator`, `ManualTaxCalculator`, `StripeTaxConfigService`, `EUVatRates`/`USSalesTaxRates`/`OtherCountryRates`); `Infrastructure.Documents/Pdf/Invoice/` VAT block
- API/UI: `TaxController.cs`, `TaxRatesController.cs`, `tms-portal/pages/settings/components/tenant-tax-rates-card/`, `<ui-money-with-tax>`

### Payments

- Domain: `Entities/Payment/Payment.cs` (`StripePaymentIntentId`)
- Application: `Modules/Financial/Payments/Commands/`, `Modules/Financial/Payments/Queries/`
- Infrastructure: `Infrastructure.Payments/Stripe/StripePaymentService.cs`
- API/UI: `PaymentController.cs`, `PublicPaymentController.cs`

### Payment links

- Domain: `Entities/Payment/PaymentLink.cs`
- Application: `Modules/Financial/PaymentLinks/Commands/`, `Modules/Financial/Payments/Commands/CreatePublicCheckoutSession/`, `Modules/Financial/PaymentLinks/Queries/`
- API/UI: `customer-portal/pages/payment/` (redirects to Stripe-hosted Checkout)

### Stripe subscriptions

- Domain: `Entities/Subscription/Subscription.cs`
- Application: `Modules/IdentityAccess/Subscriptions/Commands/`, `Modules/IdentityAccess/Subscriptions/Queries/`
- Infrastructure: `Infrastructure.Payments/Stripe/StripeSubscriptionService.cs`
- API/UI: `SubscriptionController.cs`, `admin-portal/pages/subscriptions/`, `tms-portal/pages/subscription/`

### Subscription plans

- Domain: `Entities/Subscription/SubscriptionPlan.cs`, `PlanFeature.cs`
- Application: `Modules/IdentityAccess/Subscriptions/Queries/`
- Infrastructure: `Infrastructure.Payments/Stripe/StripePlanService.cs`
- API/UI: `admin-portal/pages/plans/`

### Stripe Connect

- Domain: `Entities/Tenant.cs` (`StripeConnectedAccountId`), `Entities/Employee.cs` (`Address`, `StripeConnectedAccountId`)
- Application: `Modules/Financial/StripeConnect/Commands/`, `Modules/Financial/StripeConnect/Queries/`, `Modules/Financial/Payroll/Commands/SetupEmployeePayout/`
- Infrastructure: `Infrastructure.Payments/Stripe/StripeConnectService.cs`, `StripeCapabilities.cs` (country-aware capability map: SEPA/iDEAL/Bacs/ACH)
- API/UI: `StripeConnectController.cs`, `tms-portal/pages/settings/billing-settings/`

### Stripe webhooks

- Application: `Modules/Integrations/Webhooks/Commands/ProcessStripEvent/` (`invoice.paid`, `customer.subscription.*`, `payment_intent.processing|succeeded|payment_failed`, `account.updated`, `checkout.session.completed`)
- Infrastructure: `Infrastructure.Payments/Stripe/`
- API/UI: `WebhookController.cs` (`/webhooks/stripe`)

### Payroll

- Domain: `Entities/Invoice/PayrollInvoice.cs`
- Application: `Modules/Financial/Payroll/` (Commands, Queries, Services)
- API/UI: `tms-portal/pages/payroll/`, `Jobs/PayrollGenerationJob.cs`

### Expenses

- Domain: `Entities/Expense/` (CompanyExpense, TruckExpense, BodyShopExpense)
- Application: `Modules/Financial/Expenses/Commands/`, `Modules/Financial/Expenses/Queries/`
- API/UI: `ExpenseController.cs`, `tms-portal/pages/expenses/`

### Billing batches (Cold Chain billing per client period)

One client's billing period handed to Accounting as one `SLC-RFV####-26-M` reference - Monday's
MONTHLY BILLING UPDATES row and nSFS's per-period reefer job. Replaces per-trip or per-workfile
billing for Cold Chain deliveries (roadmap #11). Not linked to SAS: Ops prints the summary, Accounting
bills in SAS and the invoice number is typed back.

- Domain: `Entities/Billing/` - `BillingBatch` (+ `.Status.cs`: Draft -> Locked -> Forwarded ->
  Billed, Cancelled from anything not billed), `BillingBatchTrip` (a copy of the trip's price and
  outcome counts), `BillingBatchCharge` (demurrage, backload, different location, tolls at cost;
  VATable or not), `BillingPeriod` (`For(cycle, day)`: 1-15 / 16-end, Mon-Sat, month, per trip),
  `BillableTrip` (the reader's projection). `Customer.BillingCycle` (nullable) sets the period
- ⚠️ **Lines follow their trips only while Draft.** `SyncTrips` re-copies price and outcomes;
  `Lock` syncs one last time and refuses any trip not completed, not priced or price-out-of-date,
  naming each. After that the copy is frozen: `PriceTripHandler` refuses a trip on a non-draft batch.
  Unlock (Locked/Forwarded -> Draft) and Cancel need a reason; Cancel deletes the lines and charges,
  which frees the trips, and keeps the reference used
- ⚠️ **A trip is on one batch at most**: unique index on `billing_batch_trips.trip_id`; the reader
  skips trips another batch holds. A trip's billing day is its `RatedOn`, else the local day it completed
- **SLC-RFV numbering**: `WorkfileSeries.ReeferBilling`, nSFS-style per year and branch like BKN-RFV
  (`WorkfileSeries.IsNsfsNumbered`), counter `SLC-RFV-26-M`. Taken only once a batch has trips. No
  cut-over setting yet (BKN-RFV has one in Settings > Company)
- Tolls from cash funds: `AddBillingBatchTollCharges` bills each toll / parking / passway receipt on
  the batch's trips once (`CashFundExpenseId`, filtered unique), non-VAT, skipping disallowed ones
- Application: `Modules/Financial/BillingBatches/` (`BillableTripReader` - one projection per period,
  `BillingBatchSync`, `BillingBatchEdit` - load / change / save / map for every command,
  `BillingBatchReader`); queries `GetBillingBatches`, `GetBillingBatchById`, `GetUnbilledTrips` (per
  client with a cycle or a rate agreement), `GetBillingBatchPdf`
- PDF: `Infrastructure.Documents/Pdf/Billing/` (`IBillingBatchPdfService`) - header, trips, other
  charges, Service / Non-VAT / Selling totals, Prepared by / Noted by / Received by Accounting
- API/UI: `BillingBatchController.cs` (`/billing-batches`; `Invoice.View`, every change
  `Invoice.Manage`), `tms-portal/pages/billing-batches/` (Finance > Invoicing > Billing Batches: list
  with the "Ready to bill" panel, details with trips, charges and the status actions); billing cycle on
  the customer form and edit dialog
- Seed: `ph.json` customers carry `BillingCycle` from Monday's 2026 cut-offs; `CustomerSeeder`
  backfills it on existing rows
- Tests: `Application.Tests/Billing/` (`BillingBatchTests`, `BillingPeriodTests`)
- Not yet: the client-papers checklist that gates locking, nSFS's Excel transmittal
  (`RFV_SEND_TO_BILLING.xlsx`), and suppressing the per-load `LoadInvoice` for Cold Chain loads

### Cash funds (operational fund liquidation)

Cold Chain's half-month operational fund (₱15,000 released by Accounting as an nSFS `CA-RFV…`
cash advance) and what crews paid from it - passways, parking, tolls - replacing Monday's CCL
OPERATION FUNDS LIQUIDATION board. Kept apart from `Expense`: those are vendor bills with their own
approve/pay workflow; this is petty cash liquidated in a batch.

- Domain: `Entities/CashFund/` - `CashFund` (custodian `Employee`, period, advance,
  `AdvanceReference`, Open -> Liquidated), `CashFundExpense` (optional truck / trip / payer),
  `Enums/Expense/CashExpenseCategory|CashExpenseStatus|CashFundStatus`
- ⚠️ **Receipt status per line**: WithOps -> Forwarded (dated, Monday's "Date Forward") -> Checked
  or Disallowed (reason; the crew owes it back and it stops counting). Only WithOps lines can be
  edited or deleted; liquidating needs every line reviewed; a liquidated fund is locked until
  `Reopen`. `Balance` = advance - everything not disallowed, never typed
- An expense on a trip takes the trip's truck and, unless given, its driver as payer
  (`CashExpenseReferences`)
- Topping the fund up stays an nSFS request for payment (`RFP-ADM`, "CASH ADVANCE" / "FUND
  TRANSFER"); nothing is written to nSFS
- Application: `Modules/Financial/CashFunds/` (commands, `GetCashFunds` - totals from one grouped
  tally query, `GetCashFundById`, `CashFundReader` - batched truck/trip/name lookups)
- API/UI: `CashFundController.cs` (`/cash-funds`; `Expense.View` / `.Manage` for Ops,
  `Expense.Approve` to review, liquidate, reopen), `tms-portal/pages/cash-funds/` (Finance >
  Expenses > Cash Funds)
- Truck merges repoint `cash_fund_expenses.truck_id` (`MasterDataPolicies.Truck.MergeReferences`)

### Fuel log (fill-ups per truck and fleet card)

Every fill-up typed from its receipt, replacing Monday's Fleet Gas Expenses board and the fuel
columns on CCL OPS - DISPATCH. **Not a new entity**: a fill-up is a `TruckExpense` with
`Category = Fuel`, so it also shows under All Expenses and is reviewed with the expense verbs.

- Domain: `TruckExpense.ForFuel(FuelFillUp)` / `CorrectFuel` (liters, `PricePerUnit` worked out as
  amount / liters, `FleetCard` Shell/Petron/Unioil or null for cash, optional `TripId`, odometer,
  station in `VendorName`). Correcting a flagged (Rejected) fill-up sends it back to Pending
  (`Expense.ReopenForReview`)
- ⚠️ **Checks** (`FuelFillUpChecks`): not in the future; in PHP the price per liter must be
  ₱30-150 (catches liters/amount typed into each other's column, which Monday let through); no
  second fill-up for the same truck, liters and amount on the same local day (the same receipt
  logged twice); an odometer reading must sit between the truck's readings before and after it; a
  trip must have run on that truck
- **Km per liter** (`FuelEconomy`, full-to-full): a fill-up with a reading closes a leg holding
  every liter bought since the previous reading, its own included; a fill-up at the same reading is
  a top-up whose liters carry into the next leg; readings looked back 60 days
- Application: `Modules/Financial/FuelLog/` (`RecordFuelFillUp`, `UpdateFuelFillUp`,
  `GetFuelFillUps`, `GetFuelSummary` - totals, per card net of 12% VAT for checking each card's
  statement, per truck km / km per liter; both share `IFuelLogFilter`), `FuelLogReader`
- API/UI: `FuelLogController.cs` at `/expenses/fuel` (under `/expenses` so the HTTP cache evicts
  together); verify / flag / delete / receipt go through `ExpenseController`.
  `tms-portal/pages/fuel-log/` (Finance > Expenses > Fuel Log). The receipt picker moved to
  `shared/components/documents/expense-receipt-upload`
- Not yet: genset fuel (Monday's "Generator Sets" group), importing the Monday history

### Fuel cards

- Domain: `Entities/FuelCards/` (`FuelCardProviderConfiguration`, `FuelCard`, `FuelCardTransaction`); `TruckExpense.PurchaseJurisdiction`/`FuelCardProvider`/`ExternalTransactionId`
- Application: `Modules/Integrations/FuelCards/` (Commands, Queries, `Services/FuelCardSyncService`)
- Infrastructure: `Infrastructure.Integrations.FuelCards/` (WEX, EFS, Demo; factory). WEX/EFS share `Providers/BearerFuelCardService.cs`; config binds once from the `FuelCards` section into aggregate `FuelCardsOptions` (nullable `Wex`/`Efs`, read as `options.Value.Wex ?? new WexOptions()`) - same shape as `LoadBoardOptions`/`EldOptions`
- API/UI: `FuelCardsController.cs` (providers, sync, transaction review queue), `Jobs/FuelCardSyncJob.cs`, `tms-portal/pages/fuel-cards/`
- **Trap**: `FuelCard` has no endpoints by design - it's an internal card→truck mapping table written by `FuelCardSyncService` and `AssignFuelCardTransactionTruckHandler`. It looks orphaned; deleting it breaks both

### Accounting sync (QBO)

- Domain: `Entities/Accounting/` (`AccountingProviderConfiguration`, `QboEntityMapping`)
- Application: `Modules/Integrations/Accounting/` (Commands: Connect/DisconnectQuickBooks; Queries: GetQuickBooksConnection/AuthUrl), `Application.Abstractions/Accounting/` (`IAccountingProviderService`, `IAccountingProviderFactory`, `IOAuthStateProtector`)
- Infrastructure: `Infrastructure.Integrations.Accounting/` (QBO OAuth2 + REST push: Customer/Invoice/Payment/Purchase); token cols encrypted via `Persistence/Converters/EncryptedStringConverter`
- API/UI: `AccountingController.cs` (+ `/accounting/quickbooks/callback`), `Jobs/AccountingSyncJob.cs`, `tms-portal/pages/settings/integrations-settings/` + `components/quickbooks-card/`

## Communication

### Real-time messaging

- Domain: `Entities/Messaging/Conversation.cs`, `Message.cs`
- Application: `Modules/Integrations/Messaging/Commands/`, `Modules/Integrations/Messaging/Queries/`
- Infrastructure: `Infrastructure.Communications/SignalR/Hubs/ChatHub.cs`
- API/UI: `MessageController.cs`, `tms-portal/pages/messages/`

### Live tracking

- Application: `Modules/Operations/Tracking/Commands/`, `Modules/Operations/Tracking/Queries/`
- Infrastructure: `Infrastructure.Communications/SignalR/Hubs/TrackingHub.cs`, `Routing/Geocoding/` (Mapbox), `Routing/TripTrackingService.cs`
- API/UI: `TrackingController.cs`, `customer-portal/pages/tracking/`

### Notifications

- Domain: `Entities/Notification.cs`
- Application: `Modules/Platform/Notifications/Queries/`, `Modules/Integrations/UpdateNotifications/Commands/`, `Application.Abstractions/Notifications/INotificationService.cs` (port)
- Infrastructure: `Infrastructure.Communications/Notifications/` (Firebase push), `SignalR/Hubs/NotificationHub.cs`
- API/UI: `NotificationController.cs`, `tms-portal/pages/notifications/`

### Email

- Infrastructure: `Infrastructure.Communications/Email/` (Resend, Fluid templates)

### Public tracking links

- Domain: `Entities/TrackingLink.cs`
- Application: `Modules/Operations/Tracking/Commands/`

## Identity & access

### Users

- Domain: `Entities/User.cs`, `UserTenantAccess.cs`
- Application: `Modules/IdentityAccess/Users/` (Commands, Queries, Services, Specifications)
- API/UI: `UserController.cs`, `admin-portal/pages/users/`, `tms-portal/pages/settings/`

### Roles / permissions

- Domain: `Entities/Role/` (AppRole, TenantRole, claims); `Shared.Identity` `AppRoles`/`AppRolePermissions` (SuperAdmin, Admin)
- Application: `Modules/IdentityAccess/Roles/Queries/`
- API/UI: `RoleController.cs`

### App admins

- Domain: `Entities/Invitation.cs` (`Type=AppUser`, `AppRole`, nullable `TenantId`)
- Application: `Modules/IdentityAccess/Admins/` (Commands: AddAdmin/RevokeAdmin/Resend+CancelAdminInvitation, Queries: GetAdmins/GetAdminInvitations)
- API/UI: `AdminController.cs` (`/admins`, SuperAdmin-only), `admin-portal/pages/admins/`

### Invitations

- Domain: `Entities/Invitation.cs`
- Application: `Modules/IdentityAccess/Invitations/Commands/` (tenant-scoped: Employee/CustomerUser)
- API/UI: `InvitationController.cs`; app-admin invites via `AdminController`

### Auth / OAuth2

- API/UI: `Trackwise.IdentityServer/` (Duende), all portal `pages/login/`

### Tenants

- Domain: `Entities/Tenant.cs`
- Application: `Modules/IdentityAccess/Tenants/Commands/`, `Modules/IdentityAccess/Tenants/Queries/`
- Infrastructure: `Infrastructure.Persistence/Services/Tenant/CurrentTenantAccessor.cs` (`ICurrentTenantAccessor`, resolves the current tenant), `TenantDatabaseService.cs`
- API/UI: `TenantController.cs`, `admin-portal/pages/tenants/` (+ `admin-portal/shared/components/tenant-form/`)
- Demo tenants (`us`, `eu`, `solo`) come from the `Tenants[]` config array read by `DbMigrator/Seeders/Infrastructure/DemoTenantsSeeder.cs`; see [environment-variables.md](../docs/configuration/environment-variables.md#demo-tenants-tenants)

### Impersonation

- Domain: `Entities/ImpersonationToken.cs`, `ImpersonationAuditLog.cs`
- API/UI: `ImpersonationController.cs`

### API keys (MCP)

- Domain: `Entities/ApiKey.cs`
- Application: `Modules/IdentityAccess/ApiKeys/Commands/`, `Modules/IdentityAccess/ApiKeys/Queries/`
- Infrastructure: `Trackwise.McpServer/Authentication/`
- API/UI: `ApiKeysController.cs`

### Feature flags

- Domain: `Entities/Feature/DefaultFeatureConfig.cs`, `TenantFeatureConfig.cs`
- Application: `Modules/IdentityAccess/Features/Commands/`, `Modules/IdentityAccess/Features/Queries/`, `Application.Abstractions/Features/IFeatureService.cs` (port)
- Infrastructure: `Infrastructure.Persistence/Services/Feature/FeatureService.cs`
- API/UI: `FeaturesController.cs`, `TenantFeaturesController.cs`, `admin-portal/pages/features/`

## Documents & storage

### Documents (POD, BOL, …)

- Domain: `Entities/Document/` (Load, Truck, Employee, Delivery)
- Application: `Modules/Integrations/Documents/Commands/`, `Modules/Integrations/Documents/Queries/`
- Infrastructure: `Infrastructure.Documents/`
- API/UI: `DocumentController.cs`, `customer-portal/pages/documents/`

### PDF generation

- Application: `Application.Abstractions/Documents/` (`IInvoicePdfService`, `IIftaReportPdfService`, … ports)
- Infrastructure: `Infrastructure.Documents/Pdf/` (QuestPDF: invoices, payroll, BOL, POD)

### PDF import / extraction

- Application: `Modules/Operations/Loads/Commands/ImportLoadFromPdf/`, `Application.Abstractions/Documents/IPdfDataExtractor.cs`
- Infrastructure: `Infrastructure.Documents/PdfImport/LlmPdfDataExtractor.cs` (LLM via `ILlmClient`; PdfPig text + Anthropic vision fallback)
- API/UI: `LoadController.ImportFromPdf` (`POST /loads/import`)

### VIN decoder

- Application: `IVinDecoderService.cs`
- Infrastructure: `Infrastructure.Vin/` (composite: NHTSA `DecodeWMI` for prefix/country + NHTSA `DecodeVinValues` for full US detail)
- API/UI: `truck-form` Decode VIN button + source badge

### Blob storage

- Application: `IBlobStorageService.cs`
- Infrastructure: `Infrastructure.Storage/Providers/` (Azure, Cloudflare R2, file system)
- API/UI: selected via `BlobStorage:Type` config (`azure` / `r2` / default)

## Load board

### Load board search

- Domain: `Entities/LoadBoard/LoadBoardListing.cs`, `LoadBoardConfiguration.cs`
- Application: `Modules/Integrations/LoadBoard/Commands/`, `Modules/Integrations/LoadBoard/Queries/`
- Infrastructure: `Infrastructure.Integrations.LoadBoard/` (DAT, Truckstop, 123Loadboard, Demo)
- API/UI: `LoadboardController.cs`, `tms-portal/pages/loadboard/`

### Broker credit

- Domain: `Entities/LoadBoard/BrokerCreditRecord.cs`, `LoadBoardListing.BrokerCredit*`, `TenantSettings.MinBrokerCreditScore`
- Application: `Modules/Integrations/LoadBoard/Queries/GetBrokerCredit/`, booking gate in `Commands/BookLoadBoardLoad/`
- Infrastructure: `Infrastructure.Integrations.LoadBoard/Credit/` (`BrokerCreditService`, `FmcsaClient`); `Infrastructure.AI/Tools/LoadBoard/CheckBrokerCreditTool.cs`
- API/UI: `GET /loadboard/brokers/{mc}/credit`, credit badges on `tms-portal/pages/loadboard/`, threshold on `settings/company-settings/`

### Posted trucks

- Domain: `Entities/LoadBoard/PostedTruck.cs`
- Application: `Modules/Integrations/LoadBoard/Commands/PostTruck*`
- API/UI: (under `load-board/`)

### Background sync

- API/UI: `Jobs/LoadBoardSyncJob.cs`

## Settings & integrations

### Tenant settings

- Domain: `Entities/Tenant.cs` → `TenantSettings` VO; `McNumber`/`VatNumber`/`EoriNumber`/`CompanyRegistrationNumber`/`TaxResidencyCountry`; `OperatingMode` (see _Solo / owner-operator mode_)
- Application: `Modules/IdentityAccess/Tenants/Commands/UpdateTenant` (regulatory IDs validated server-side via `RegexPatterns.VatNumber`/`McNumber`/`EoriNumber`; `AIEnabled` and `BlockAIOverage` toggles per tenant)
- API/UI: `tms-portal/pages/settings/company-settings/`

### System settings

- Domain: `Entities/SystemSettings.cs`
- Application: `ISystemSettingsService.cs`

### Captcha

- Application: `Application.Abstractions/Captcha/ICaptchaService.cs` (port)
- Infrastructure: `Infrastructure.Communications/Captcha/` (Google reCAPTCHA)

### Geocoding

- Application: `Application.Abstractions/Geocoding/IGeocodingService.cs` (port)
- Infrastructure: `Infrastructure.Routing/Geocoding/` (Mapbox; biased to `Tenant.Settings.Region` country + language)

### Trip optimization

- Application: `ITripOptimizer.cs`
- Infrastructure: `Infrastructure.Routing/Optimization/` (heuristic, Mapbox Matrix, composite - Matrix costs are overridden for PH inter-island pairs, see Ferry-aware routing)

### Ferry-aware routing

Mapbox's Directions/Matrix APIs are driving-only and fabricate a road route across open ocean for
a PH inter-island pair (e.g. Manila -> Davao) instead of erroring - this resolves the real
drive+ferry route for the small port network Skybest's branches sit on (Luzon/Cebu/Negros/Mindanao).

- Application: `Application.Abstractions/Routing/IFerryRouteResolver.cs` (port); `Modules/Operations/Routing/Queries/GetRouteSegments/`
- Infrastructure: `Infrastructure.Routing/Ferry/` (`PhilippineFerryNetwork` static port/crossing data, `FerryRouteResolver` island bounding-box classification + Dijkstra over the port graph)
- API/UI: `RouteController.cs` (`GET routes/segments`); `tms-portal/shared/components/maps/direction-map/direction-map.ts` (`buildSegments`, calls this endpoint instead of Mapbox Directions directly, draws `ferry` legs as a dashed line)
- Consumed by: `MapboxMatrixTripOptimizer` (overrides fabricated Matrix costs for cross-island pairs), `OptimizeTripStopsHandler` (ferry-aware `TotalDistance`), `load-form.ts`'s `distance` field (via `direction-map`'s `routeChange`)

## Marketing site

### Blog

- Domain: `Entities/BlogPost.cs`
- Application: `Modules/Platform/BlogPosts/Commands/`, `Modules/Platform/BlogPosts/Queries/`
- API/UI: `BlogPostController.cs`, `admin-portal/pages/blog-posts/`, `website/`

### Contact form

- Domain: `Entities/ContactSubmission.cs`
- Application: `Modules/Platform/Contacts/Commands/`, `Modules/Platform/Contacts/Queries/`
- API/UI: `ContactController.cs`, `admin-portal/pages/contact-submissions/`

### Demo requests

- Domain: `Entities/DemoRequest.cs`
- Application: `Modules/Platform/DemoRequests/Commands/`, `Modules/Platform/DemoRequests/Queries/`
- API/UI: `DemoRequestController.cs`, `admin-portal/pages/demo-requests/`

### Telegram bot

- Domain: `Entities/TelegramChat.cs`, `TelegramLoginState.cs`
- API/UI: `Trackwise.TelegramBot/`

## Freight Forwarding Features

### Tariff

-Domain: `Entities/Freightforwarding/Tariff.cs`
-Applications: `Application/Modules/operations/FreightForwarding`
-API/UI: `Trackwise.Api/TariffController`

- Effective-dated versions can be client-scoped; quotation rating prefers a matching client tariff
  before contract-scoped and general tariffs.

### Rate agreements (Cold Chain truck-trip pricing)

A client's contracted truck-trip rates for a period - what each Monday quotation board held. Kept
apart from `Tariff` (forwarding lane x container): these are keyed by place or distance band and
truck size, with fuel on top.

- Domain: `Entities/Pricing/` - `RateAgreement` (Draft -> Active -> Superseded/Withdrawn; a price
  change is `CreateRevision`, never an edit), `RateAgreementLine` (a place - city and/or province -
  xor a km band; one truck size or any), `RateFuelSurcharge` (per diesel tier), `DieselPrice`
  (effective-dated, one per day), `RatePricer` (pure pricing rules), `Trip.Pricing.cs` (snapshot)
- **Quotation terms** (`RateAgreement.Terms.cs`, passed as the `RateAgreementTerms` record): drops
  the rate covers (`IncludedDrops`, null = one), `RateExtraDropCharge` rows per truck size (null
  size = any), foul-trip and backload as a **percentage** of the rate, demurrage free hours and
  hourly rate, a different-location charge, and whether tolls and parking are billed at cost. Null
  means the quotation doesn't say. Pricing applies only the included drops, the per-size fee and
  the foul-trip share (when every drop was a foul trip or cancelled by the client); backload,
  demurrage and the location charge are recorded for billing, because ops bill them case by case
- **Billing switches** (`BillExtraDrops`, `RatesIncludeVat`): Skybest's billing practice rather than
  the client's price, so `SetBillingRules` changes them on an approved card without a revision
  (`PUT /rate-agreements/{id}/billing-rules`, details page "Billing" card). Extra drops off = counted
  but free (SAS bills none); VAT-inclusive = rate, fuel and extra drops billed ÷1.12
- **Route costing** (`RouteCosting`, `GET /rate-agreements/costing`, page `rate-agreements/route-costing`):
  Cold Chain's Monday costing board as a calculator - fuel by km/L and diesel, crew, 30% admin,
  markup by size, buffer, round up. Defaults per size in `RouteCosting.DefaultsFor`, each overridable
- ⚠️ **The price is per truck-trip, and loads are per drop.** `PriceTripCommand` charges the
  costliest drop's line (+ fuel) and `ExtraDropCharge` per other distinct drop, and **writes those
  amounts onto the drop loads' `DeliveryCost`**, so `Trip.CalcTotalRevenue` stays the trip price.
  Re-pricing overwrites them. The trip keeps a snapshot (`RateAgreementId`, base/fuel/extra, `RatedOn`).
- Rows are **destinations**; the origin is assumed to be the client's usual warehouse (GNI's km
  bands read the load's route distance). Place matching is `RatePricer.NormalizePlace`; a town line
  beats a province line.
- Application: `Modules/Operations/RateAgreements/` (commands, queries, `TripRateCalculator`,
  `RateAgreementReader` - batches surcharges, reading lines' navigations would be N+1)
- API: `RateAgreementController` (`/rate-agreements`), `DieselPriceController` (`/diesel-prices`),
  `TripController` `GET trips/{id}/rate-quote`, `POST trips/{id}/price`. Guarded by
  `Permission.Tariff.*` (Owner/Manager); pricing a trip needs `Load.Manage`
- UI: `tms-portal/pages/rate-agreements/` (list + diesel price card, details, add/edit with the rate
  grid editor), `pages/trips/components/trip-rate-card`
- Seed: `RateAgreementSeeder` from `SeedData/ph-rate-agreements.json`, built by
  `Documents\monday-export\scripts\rates.py` from ten clients' quotation boards (Grandlink has none).
  Monday revisions list only changed rows, so rows a revision omits are **carried over** and named
  in its Notes. Monday keeps only the rate table, so each client's terms come from its latest
  quotation PDF (`TERMS` in the script); Frabelle, Superb and Yokka have none on file. Superb's
  "Additional ₱300" is the July 2022 rate increase, not an extra-drop charge.
- Tests: `Trackwise.Application.Tests/Pricing/`

### ChargeCode

-Domain: `Entities/Freightforwarding/ChargeCode.cs`
-Applications: `Application/Modules/operations/FreightForwarding`
-API/UI: `Trackwise.Api/ChargeCodeController`

### Quotation

-Domain: `Entities/Freightforwarding/Quotation.cs`, `QuotationLine.cs`
-Applications: `Application/Modules/operations/FreightForwarding` (`CreateQuotationRevision` creates a new draft in the same revision family, preserving the customer, freight mode, route, and charge setup)
-API/UI: `Trackwise.Api/QuotationController`, `tms-portal/pages/forwarding/quotation/`

### Booking

An accepted (Approved) Quotation converts into a Booking - the trackable job against a carrier.
Status: `Pending -> Confirmed -> InExecution -> Completed`, with `Cancelled` reachable from
`Pending`/`Confirmed`. A `Load` can only be created against a `Confirmed`/`InExecution` Booking
(`Load.BookingId`, nullable for pre-existing Loads) - enforced in `LoadService.CreateLoadsAsync`,
not the DB.

- Domain: `Entities/Freightforwarding/Booking.cs` (links back to `Quotation`; acceptance creates the
  initial Booking, and an accepted quotation can explicitly create additional bookings with distinct
  booking numbers and copied price snapshots)
- Application: `Application/Modules/Operations/FreightForwarding/Commands/` (`ConvertQuotationToBooking`, `ConfirmBooking`, `StartBookingExecution`, `CompleteBooking`, `CancelBooking`), `Queries/` (`GetBookingById`, `GetBookings`)
- API/UI: `BookingController.cs` (`/bookings`), `QuotationController.ConvertToBooking` (`POST /quotations/{id}/convert-to-booking`), `tms-portal/pages/forwarding/booking/`

**Cold Chain bookings** are the same entity made from a client's rate agreement instead of a
quotation (`QuotationId` null, `RateAgreementId` set): one truck-trip for one client on one day,
what Monday's CCL CS - BOOKING MONITORING row holds. They carry a BKN-RFV reference from
the reefer workfile series (`IWorkfileReferenceGenerator`), and never get a workfile (Cold Chain
bills in batches per client billing period, roadmap #11).

- ⚠️ **BKN-RFV continues Skybest's nSFS numbering** (decided 2026-10-05): formatted as nSFS prints it,
  `BKN-RFV1514-26-M` (4-digit minimum, year the booking is entered, branch fixed to `M` until bookings
  carry a branch - `WorkfileSeries.ReeferBranch`), from a counter per year and branch
  (`WorkfileNumberSeries.CounterKey` -> row `BKN-RFV-26-M`), so January starts at 1 by itself. The other
  series keep `BKN-IMP-000123` and one counter. Reefer workfiles share the series and the format
- **Setting the next number at cut-over**: Settings > Company > "Cold Chain booking numbers"
  (`settings/components/booking-numbering-card`, Owner only via `Tenant.Manage`; Manager sees it).
  `GET/PUT /bookings/numbering` (`GetBookingNumbering`, `SetNextBookingNumber`) ->
  `IWorkfileReferenceGenerator.TrySetNextAsync`, one guarded UPSERT that never moves the counter back,
  so a number already issued can't be issued again. The old single `BKN-RFV` counter row is unused

- Domain: `Booking.RateAgreement.cs` (service date, call time, truck size, pickup site, commodity,
  temperature range, the saved estimate), `BookingDrop` (one saved client delivery location per
  drop), `ColdChainBookingPlan`. A cancelled or lost Cold Chain booking must carry a
  `BookingLossReason` (plus `CancellationParty` on a cancel); forwarding bookings don't.
- ⚠️ **Estimate and trip price read the same inputs.** `BookingRateEstimator` (in
  `Modules/Operations/RateAgreements/`) prices a plan with `RatePricer`, the agreement and diesel
  price of the service date (`RateAgreementLookup`, shared with `TripRateCalculator`), and the
  straight-line km from the pickup warehouse to each drop. The trip made from the booking passes the
  same km as each load's `Distance`, so a distance-band card (GNI) prices the trip as estimated.
- `CreateTripFromBooking` builds one load per drop and sends a `CreateTripCommand` (crew from the day's
  crew plan), then prices the trip, all in one transaction; the booking goes to InExecution. The
  trip completing completes the booking (`TripCompletedHandler`); cancelling it puts the booking back
  to Confirmed (`CancelTripHandler`).
- **Standing orders** (`RepeatColdChainBooking`): copies a booking's plan (`Booking.PlanOn`) onto
  chosen days, each a new booking with its own BKN-RFV number and estimate, optionally confirmed.
  All or nothing in one transaction - a day the agreement doesn't cover fails the lot.
- **Planning board** (`GetUnassignedBookings`): one day's Cold Chain bookings still Pending or
  Confirmed, which is exactly the ones with no trip (making the trip moves a booking to
  InExecution, cancelling it moves it back). Shown on Crew Plan's Bookings tab.
- API: `POST /bookings` (create), `PUT /bookings/{id}`, `POST /bookings/estimate`,
  `POST /bookings/{id}/trip` (needs `Booking.Manage` and `Load.Manage`),
  `POST /bookings/{id}/repeat`, `GET /bookings/unassigned?date=`
- UI: `cold-chain-booking-add` / `-edit` with `cold-chain-booking-form` (live estimate), and on
  details `cold-chain-booking-panel`, `repeat-booking-dialog`, `booking-loss-dialog`; the shared
  `shared/components/domain-forms/create-trip-dialog` (booking details and the Crew Plan board)
- Tests: `Application.Tests/FreightForwarding/ColdChainBookingTests`, `RepeatColdChainBookingHandlerTests`,
  `GetUnassignedBookingsHandlerTests`, `Pricing/BookingRateEstimatorTests`

### BusinessPartner

The counterparty master for forwarding files - shipping line, co-loader, forwarder, trucker,
broker, agent, shipper, consignee, depot. Distinct from `Customer` (who gets billed): the shipper
on a file is often not the paying customer, and one partner can hold several roles at once.
A workfile slot only accepts a partner that is `IsActive` **and** holds the matching role
(`BusinessPartner.CanActAs`).

`Roles` is a Postgres `text[]` of snake_case values, not `ChargeCode`'s JSON-text array, so
`Roles.Contains(role)` still translates to SQL for the header's partner pickers.
`SnakeCaseEnumConvention` only reaches scalar enum properties, so `BusinessPartnerConfiguration`
applies the element converter by hand - guarded by
`Trackwise.Infrastructure.Persistence.Tests/Configurations/BusinessPartnerRoleQueryTranslationTests.cs`.

- Domain: `Entities/FreightForwarding/BusinessPartner.cs`, `Enums/FreightForwarding/BusinessPartnerRole.cs`
- Application: `Application/Modules/Operations/FreightForwarding/Commands/BusinessPartner/` (`CreateBusinessPartner`, `UpdateBusinessPartner`, `DeleteBusinessPartner`), `Queries/BusinessPartner/` (`GetBusinessPartnerById`, `GetBusinessPartners`)
- API/UI: `BusinessPartnerController.cs` (`/business-partners`)

### Workfile

The post-booking operational and pre-billing file: `Booking -> Workfile -> Billing`. A confirmed
booking is promoted into one via `POST /bookings/{id}/promote-to-workfile` (creation-by-conversion
lives on the source aggregate, like `QuotationController.ConvertToBooking`), and the unique
**filtered** index on `BookingId` makes that one-way and one-time.

**Shipment type is a TPH discriminator, not a flag** - `RegularWorkfile` (ocean, with a
`FreightMode Direction`), `ReeferWorkfile`, `TruckingWorkfile` (with a `TruckingVariant`), sharing
the `workfiles` table. The legacy system decided behaviour by string-matching reference prefixes;
here the per-type rules live in `WorkfileRules.For(type)` and the reference series in
`WorkfileSeries.For(...)`. Both are **static** so promotion can ask what a type requires before it
has anything to construct, and `Workfile` exposes them as properties that delegate to the same
source. **Reefer is the odd one out on three rules** - no booking, no MBL/HBL indicator, no
duplicate-BL check - and getting any of them wrong rejects valid files with no other symptom.

Status runs `Open -> ManagerReview -> AccountingReview -> Closed`, with `Cancelled` from
`Open`/`ManagerReview` and `ManagerReview -> Open` on a disapproval. Unlike `Booking`, illegal
transitions throw (`WorkfileStatusMachine`), so every handler checks `CanTransition` first and
returns a message rather than letting the domain throw. `IsReferenceLocked` is **orthogonal to
status**: a pending reference-closure request freezes every write wherever the file sits, and only
Accounting releases it.

**One verb per stage, because the file changes hands.** Each transition has its own endpoint and
its own permission - `Manage` alone is never enough to move a file forward past ops:

| Move                       | Transition                            | Endpoint                                     | Permission                |
| -------------------------- | ------------------------------------- | -------------------------------------------- | ------------------------- |
| Submit for approval        | `Open -> ManagerReview`               | `POST /workfiles/{id}/submit-for-approval`   | `Workfile.Manage`         |
| Disapprove                 | `ManagerReview -> Open`               | `POST /workfiles/{id}/return-to-ops`         | `Workfile.Approve`        |
| Sign off to Accounting     | `ManagerReview -> AccountingReview`   | `POST /workfiles/{id}/forward-to-accounting` | `Workfile.ForwardBilling` |
| Close                      | `AccountingReview -> Closed`          | `POST /workfiles/{id}/close`                 | `Workfile.Approve`        |
| Cancel                     | `Open`/`ManagerReview` `-> Cancelled` | `POST /workfiles/{id}/cancel`                | `Workfile.Manage`         |
| Request reference closure  | locks, no status change               | `POST /workfiles/{id}/request-closure`       | `Workfile.Manage`         |
| Release a locked reference | unlocks, no status change             | `POST /workfiles/{id}/clear-closure`         | `Workfile.ClearClosure`   |

**Close is the one write a locked file still accepts** - the closure request locks the file
precisely so Accounting can finish it, so `CloseWorkfileHandler` skips the `IsReferenceLocked`
guard every other handler applies. Getting that backwards deadlocks the file with no way out.

Two things the status machine still cannot express: Accounting has no way back (`AccountingReview`
only goes to `Closed`), and `MarkForwardedToAccounting` sets `IsForwardedToAccounting` without any
charges to forward - once workfile charges exist, marking them belongs in
`ForwardWorkfileToAccountingHandler`.

References are allocated per series (`BKN-DOM/IMP/EXP/TRI`, `BKN-RFV`, `BKN-TRK/TRL`) from
`workfile_number_series` by `IWorkfileReferenceGenerator`, as one atomic
`INSERT ... ON CONFLICT DO UPDATE ... RETURNING`. Read-then-write would hand two files opened in the
same second the same number. Gaps are acceptable, collisions are not.

**Genset add-on (Reefer only)** - `WorkfileRules.SupportsGenset`, per
[COLD_CHAIN_LOGISTICS_CONTEXT.md](handoff/COLD_CHAIN_LOGISTICS_CONTEXT.md) §3: genset has never been
seen booked standalone, always attached to an existing reefer file, so it is its own verb
(`SetGensetAddOn`/`RecordGensetHoursUsed`/`RemoveGensetAddOn` in `Workfile.Genset.cs`) rather than a
header field. Billed one of two shapes (`GensetBillingType`): `Itemized` (hourly rate x hours
actually used, recorded as the job runs) or `ProfitShare` (a flat cut when a partner/co-loader
fronts the job - hours don't apply). `GensetId` optionally names a `Genset` master-data unit;
assignment routinely lags the booking in the source data, so it stays nullable independently of the
billing fields. Endpoints: `PUT/DELETE /workfiles/{id}/genset`, `POST /workfiles/{id}/genset/hours`,
all `Workfile.Manage` - same editability gate (`IsEditable`) as the header, no separate permission.

**Container slots (every type)** - `WorkfileContainer`, one row per box on this move, step 1 of
[FORWARDING_LEGS_PLAN.md](handoff/FORWARDING_LEGS_PLAN.md). Size is required, the box is not:
forwarding books "4 x 40HC" first and only 7% of nSFS forwarding rows ever carry a number, so
`ContainerId` stays null until the truck leg knows it. Seal, gross kg, packages and CBM live here
because they are facts of one move - `Container` (the register) no longer carries seal, booking,
B/L or weight. Typing a number finds the box in the register or **adds it there**
(`Workfile/Containers/WorkfileContainerBoxes.cs`); a box registered under another size is refused
naming that size. Endpoints: `POST /workfiles/{id}/containers` (with `Quantity`),
`PUT|DELETE /workfiles/{id}/containers/{containerId}` - `containerId` is the slot id, not the box's.
UI: `components/workfile-containers` (panel) + `workfile-container-dialog`; the container details
page lists the box's shipments (`ContainerDto.Shipments`).

**Truck legs (Domestic/Import/Export)** - step 2 of the plan. A leg is a `TruckingWorkfile` with
`ParentWorkfileId` + `LegRole` (Pickup/Delivery/Transfer), numbered on its own `SLC-TRK####-yy-M`
counter (`WorkfileSeries.TruckLeg`, nSFS-numbered, Manila only until workfiles carry a branch) and
never billed on its own - the parent's bill covers it. `AddTruckLeg` creates the leg plus one
**uninvoiced** `Load` per parent slot (`Load.WorkfileId` = the leg, `Load.WorkfileContainerId` = the
slot, so the box number is read through the slot); each end is a port (`Terminal`, geocoded from its
address - it has no stored pin) or a client site (`Workfile/Legs/TruckLegEnds.cs`). A slot can't be on
two live legs of the same role; cancelling a leg cancels its undispatched loads and frees its slots,
and is refused once a box is picked up. `RegularWorkfile.SupportsTruckLegs` is false for Triangular.
Endpoint: `POST /workfiles/{id}/legs`. Read side: `TruckLegReader` fills `WorkfileDto.Legs` on the
parent and `ParentReferenceNumber` / `LegLoads` on a leg. UI: `components/workfile-legs` (panel) +
`workfile-leg-dialog`, and `workfile-leg-loads` on a leg's own page.

- Domain: `Entities/FreightForwarding/Workfile.cs` (+ `.Status`, `.Header`, `.Events`, `.Genset`,
  `.Containers`, `.Legs` partials), `WorkfileContainer.cs`, `WorkfileContainerSpec.cs`, `RegularWorkfile.cs`, `ReeferWorkfile.cs`, `TruckingWorkfile.cs`, `WorkfileRules.cs`,
  `WorkfileSeries.cs`, `WorkfileFactory.cs`, `WorkfileStatusMachine.cs`, `WorkfileNumberSeries.cs`,
  `GensetBillingType` enum (`Domain.Primitives/Enums/FreightForwarding/`)
- Application: `Application/Modules/Operations/FreightForwarding/Commands/Workfile/`
  (`PromoteBookingToWorkfile`, `UpdateWorkfileHeader`, `CancelWorkfile`,
  `SubmitWorkfileForApproval`, `ReturnWorkfileToOps`, `ForwardWorkfileToAccounting`,
  `CloseWorkfile`, `RequestWorkfileReferenceClosure`, `ClearWorkfileReferenceClosure`,
  `SetWorkfileGensetAddOn`, `RecordWorkfileGensetHours`, `RemoveWorkfileGensetAddOn`),
  `Queries/Workfile/`
  (`GetWorkfileById`, `GetWorkfiles`), `Workfile/Services/WorkfilePartnerValidation.cs`
  (every named counterparty must be active **and** hold the matching role)
- Infrastructure: `Infrastructure.Persistence/Services/FreightForwarding/WorkfileReferenceGenerator.cs`
  (port in `Application.Abstractions/Modules/Operations/FreightForwarding/`)
- API/UI: `WorkfileController.cs` (`/workfiles`), `BookingController.PromoteToWorkfile`,
  `tms-portal/pages/forwarding/workfile/` (genset add-on panel not yet built - see Master Data
  Management's `Genset` entry for the unit registry it references)

## Master Data (MDM)

Scope, build order and current status: [MASTER_DATA_PLAN.md](handoff/MASTER_DATA_PLAN.md). This map stays
the answer to _where the code is_; that file answers _where we are going_.

The canonical source of truth for reference data that nSFS, iWMS, ERS, SAS and this platform each
kept their own drifting copy of. Extends the existing entities (`BusinessPartner`, `Terminal`,
`Truck`, `Employee`, `ChargeCode`, `Tariff`) rather than duplicating them - a parallel master set
would recreate the problem the module exists to remove.

**Foundations, the reference lists, partners and all of fleet are in; only rates are not.** `Branch`,
`Terminal`, `Vessel`, `Commodity`, `Location`, `ContainerType`, `Currency`, `BusinessPartner`,
`Trailer`, `Helper`, `Truck` and `Employee` implement `IMasterDataRecord`. `Branch` is the reference
slice to copy, `Terminal` (ports) is the worked example of promoting an entity that **already had
callers**, `Truck`/`Employee` are the worked example of promoting one with a **much bigger** existing
surface (~59 Application-layer call sites for Truck alone) plus, for `Employee`, live-auth risk on
top - see MASTER_DATA_PLAN.md §6 for what that promotion found and fixed, `Location` is the one to
copy for anything self-referencing, `ContainerType` is the one to copy when a list has to replace an
**enum** other tables already store, `BusinessPartner` is the one to copy for anything that implements
`IMasterDataApprovable` - it is the only one of the twelve that also does - and `Trailer`/`Helper` are
the ones to copy for a brand-new entity with no legacy table and no dependents yet. See the plan for
the milestone order (reference lists -> partners -> fleet reference lists -> promoting Truck/Employee
-> rates) and for why Fleet split into a "new tables" half and a much higher-risk "promote an existing
entity" half.

⚠️ **`DuplicateProbe`'s raw SQL assumed every entity's code column is literally named `code`.** True
for eleven of twelve; `Truck` kept its DB column `number` (never renamed - see `Truck.Number`'s doc
comment) and only forwards `Code` at the C# level, so Truck creation 500'd until a live browser pass
caught it (2026-09-18) - invisible to the unit test suite, since this path needs a real Postgres
connection for `similarity()`. Fixed via `MasterDataPolicy.CodeColumn`. See MASTER_DATA_PLAN.md §8 for
the full trap writeup before promoting the next entity with a non-standard column.

⚠️ **Import and merge did not run until 2026-09-15.** `EnableRetryOnFailure` installs an execution
strategy that refuses any transaction its caller opens itself, which is what both did. Fixed by
moving both onto `ExecuteInTransactionAsync` - see "Bulk import with dry run" and "Merge and
cross-reference forwarding" below, and MASTER_DATA_PLAN.md §6c/§7 for what it cost to find.

### Master data foundations

`ISoftDeletable` and `IMasterDataRecord` (`Domain/Core/`) opt an entity into the module's
machinery. **Not to be confused with `IMasterEntity`**, the unrelated marker that routes an entity
to the master database rather than a tenant one. Traps:

- **Soft delete is not `IsActive`.** "Deactivated" is a business state a steward reverses;
  "deleted" is a record that should no longer surface. Legacy nSFS conflates both into one `stat`
  column - that conflation is absorbed by the compatibility layer and must not reach the domain.
- **`ApplySoftDeleteConventions` adds a global query filter**, so marking an _existing_ entity
  `ISoftDeletable` silently changes the results of every query already written against it. Audit
  those call sites in the same change, and use `IgnoreQueryFilters()` where a steward must see
  deleted records. `Branch` was the first such entity; before it, `HasQueryFilter` appeared nowhere
  in the repo.
- **`IBranchScoped`** (`Domain/Core/`) is the scoping marker: a nullable `BranchId`, exactly one
  branch per record, null meaning "applies everywhere". Never a join table - scoping by duplicating
  a record per office is where the legacy drift came from. ⚠️ Every table that gains a `branch_id`
  must also appear in `MasterDataPolicies.Branch.DependencyRules`, or a steward can retire a branch
  out from under the records scoped to it and the probe will report it as safe. Five tables are
  declared there today: `terminals`, `vessels`, `commodities`, `locations`, `business_partners`.
  `MasterDataPolicyTests.EveryBranchScopedEntity_IsDeclaredAsABranchDependency` enforces the pair.
- `IMasterDataRecord.Group` is an **instance** property, not `static abstract`: a static abstract
  member would bar the interface from being used as a type argument, and
  `ChangeLogSaveChangesInterceptor` walks `ChangeTracker.Entries<IMasterDataRecord>()`.
  `ApplyMasterDataConventions` keeps it out of the database.

- Domain: `Domain/Core/ISoftDeletable.cs`, `Domain/Core/IMasterDataRecord.cs`,
  `Domain/Entities/MasterData/` (`Branch`, `MasterDataXref`, `MasterDataChangeLog`,
  `MasterDataMergeHistory`, `NumberSeries`),
  `Domain.Primitives/Enums/MasterData/` (`MasterDataGroup`, `MasterDataSourceSystem`, `MasterDataChangeType`,
  `MasterDataState`, `MasterDataDeactivation`)
- Infrastructure: `Persistence/Interceptors/SoftDeleteSaveChangesInterceptor.cs` and
  `ChangeLogSaveChangesInterceptor.cs` (**registration order is the execution order** - soft delete
  rewrites the deletion, then the change log classifies it),
  `Persistence/Extensions/ModelBuilderAuditExtensions.cs` (`ApplySoftDeleteConventions`,
  `ApplyMasterDataConventions`), `Persistence/Configurations/MasterData/`,
  `Persistence/Services/MasterData/MasterDataCodeGenerator.cs`
  (port: `Application.Abstractions/Modules/MasterData/IMasterDataCodeGenerator.cs`)
- Tests: `Trackwise.Infrastructure.Persistence.Tests/MasterData/`

### Legacy cross-reference and merge

`MasterDataXref` maps `(SourceSystem, SourceTable, SourceKey) -> canonical record`. It is what makes
a merge survivable: the losing record's xref rows are **repointed** at the survivor, never deleted,
so every historical reference from every system still resolves. A merge also tombstones the loser
via `MergedIntoId` and snapshots it into `MasterDataMergeHistory` - the loser survives only as a
tombstone, so without that snapshot nobody can later tell what was folded in.

Code uniqueness is a **filtered** unique index (`WHERE deleted_at IS NULL`): a soft-deleted record
would otherwise hold its code hostage forever.

- Domain: `Domain/Entities/MasterData/MasterDataXref.cs`, `MasterDataMergeHistory.cs`

### Code generation

`MasterDataCodeGenerator` allocates in a **single atomic upsert**
(`INSERT ... ON CONFLICT (series) DO UPDATE ... RETURNING`) against `number_series`. Read-then-write
would hand the same code to two stewards saving in the same second; that atomicity, not a held
reservation, is what the module's "reservation mechanism" requires. Gaps are accepted, collisions
are not.

`WorkfileNumberSeries` + `WorkfileReferenceGenerator` solve the same problem for workfile
references with hard-coded formats (BKN-RFV's per-year nSFS format, see _Booking_). Folding the two together needs a migration of live counters -
deliberately deferred, not an oversight.

- Domain: `Domain/Entities/MasterData/NumberSeries.cs`

### Branch

The scoping entity, and the worked example of a master-data vertical slice - copy it when adding
`Location`, `Commodity`, `Vessel` and the rest.

Three things differ from an ordinary CRUD feature:

- **Delete is soft and reversible.** `DeleteBranchHandler` calls `repository.Delete` like any other
  handler; the interceptor rewrites it. `RestoreBranchCommand` undoes it, and both the restore and
  the create-clash check must `IgnoreQueryFilters()` - the filter hides the very row they target.
- **Code is immutable after creation** and blank-on-create means "allocate from the `Branch` number
  series". It is the stable handle other systems join on and it survives a merge, so a rename would
  strand every legacy key. The edit form marks it `readonly` through the Signal Forms schema
  (`readonly(p.code, {when})`) - a `[readonly]` binding alongside `[formField]` is a compile error.
- **Uniqueness is scoped to live rows**, so creating over a deleted record's code is refused with a
  message naming that record rather than a bare "already exists".

- Application: `Application/Modules/MasterData/Commands/Branch/` (`CreateBranch`, `UpdateBranch`,
  `DeleteBranch`, `RestoreBranch`), `Queries/Branch/` (`GetBranches`, `GetBranchById`),
  `Queries/ChangeLog/GetMasterDataChangeLog` (works for any master entity - keyed by entity type
  and id, not a foreign key per table)
- API/UI: `Controllers/MasterData/BranchController.cs` (`/master-data/branches`, plus
  `/{id}/history` and `/{id}/restore`), `tms-portal/pages/master-data/branch/`,
  nav in `shared/layout/sidebar/nav/master-data.nav.ts`
- Permission: `Permission.MasterData.{View,Manage}` - Owner and Manager steward, Dispatcher reads.
  Governs the whole module, not just Branch

### Ports (Terminal)

The second master entity, and the one that shows what promoting an **existing** entity costs.
`Terminal` is the port: extended in place rather than shadowed by a parallel `Port`, since a second
copy would recreate the duplication the module removes. `/master-data/ports` owns every write;
`/terminals` is read-only and feeds the dispatch pickers.

- **The write path was unified, not duplicated.** `Modules/Operations/Terminals/Commands/` is gone -
  its `DeleteTerminalHandler` hand-rolled the container and load checks the dependency probe now
  declares, and its update mutated `Code`. Leaving those endpoints would have let a steward create a
  duplicate, rename a legacy join key, and delete past a dependency, all without the pipeline.
- **Not feature-gated.** The old terminal commands carried
  `[RequiresFeature(IntermodalContainers)]`; ports are reference data forwarding and documentation
  need regardless, so the master-data routes gate on `Permission.MasterData.*` alone. The read-only
  `/terminals` routes keep `Permission.Terminal.View`.
- **First non-empty `DependencyRules`.** Containers at the port block a delete but only inform a
  deactivate - closing a port while stock drains is ordinary; deleting it while stock sits there
  contradicts what deletion means. Loads block both, narrowed to in-flight ones.
  ⚠️ Their `LiveCondition` is a **status** predicate, not `deleted_at IS NULL`: neither `containers`
  nor `loads` is soft-deletable, so the habitual condition would be a runtime SQL error on a
  steward's retirement rather than a wrong count.
- **Merging a port moves real rows.** `IMasterDataReferenceRepointer` repoints the FK columns the
  policy already declares, so one declaration serves both the retirement probe and the merge. Branch
  never needed it - nothing pointed at a branch. `MasterDataMergeService` therefore owns a
  transaction now: the repoint is immediate SQL while the tombstone is staged.
  History that must follow a merge but may not block a retirement (a truck's expenses, fuel cards,
  papers, DVIRs) goes in the policy's `MergeReferences`, never under a unique index
  (`MergeReferenceModelTests`).

⚠️ **Marking an existing entity `ISoftDeletable` changes every query already written against it.**
The two that mattered here, both display paths that must still see a retired port:
`TerminalRefResolver` (used by `LoadIntermodalResolver` and the container list) and the container
detail read. Both use `IgnoreQueryFilters()`; `ContainerMapper` no longer reads the
`CurrentTerminal` navigation at all, which also removed a long-standing N+1.

- Domain: `Domain/Entities/Terminal/Terminal.cs`, `Domain/Core/IBranchScoped.cs`
- Application: `Modules/MasterData/Commands/Port/` (`CreatePort`, `UpdatePort`, `DeletePort`,
  `RestorePort`, `MergePorts`), `Queries/Port/` (`GetPorts`, `GetPortById`),
  `Modules/Operations/TerminalRefResolver.cs`, `Modules/Operations/Terminals/Queries/` (read-only)
- Infrastructure: `Persistence/Configurations/Terminal/TerminalEntityConfiguration.cs`,
  `Persistence/Services/MasterData/MasterDataReferenceRepointer.cs`,
  migrations `AddPortsMasterData`, `AddDuplicateProbeIndexes`
- API/UI: `Controllers/MasterData/PortController.cs` (`/master-data/ports`),
  `Controllers/TerminalController.cs` (GET only), `tms-portal/pages/master-data/port/`,
  nav in `shared/layout/sidebar/nav/master-data.nav.ts`
- Permission: `Permission.MasterData.{View,Manage}`
- Tests: `Trackwise.Application.Tests/MasterData/PortCommandValidatorTests.cs`,
  `Trackwise.Application.Tests/Operations/TerminalDisplayLookupTests.cs`,
  `Trackwise.Infrastructure.Persistence.Tests/MasterData/TerminalSoftDeleteTests.cs`

### Reference lists (Vessel, Commodity, Location)

The three lists that follow ports. All new tables, all `IBranchScoped`, all importable, and all
taking the ports shape unchanged - so the interesting part is only what each adds to it.

- **Vessel** - the module's first **second exact rule**. `ImoNumber` is globally unique and never
  reassigned, so the probe matches it exactly alongside the code; a merge where both sides carry
  _different_ IMO numbers is refused outright, because those are two ships. Nullable and unique only
  among live rows _where present_ (`deleted_at IS NULL AND imo_number IS NOT NULL`) - most of the
  domestic fleet has none. Stored digits-only via `Vessel.NormalizeImoNumber`, so "IMO 9395044" and
  "9395044" probe identically.
- **Commodity** - `HsCode` is deliberately **absent from the duplicate rules**. It classifies a
  family of goods for customs rather than identifying a record, so distinct lines share one; an
  exact rule would flag every one of them. `UnNumber` is cleared whenever `IsHazardous` is false, on
  create, update and merge - a UN number on ordinary freight reads later as a classification
  somebody made.
- **Location** - the first entity that is **its own dependent**. `ParentLocationId` is a zone, and
  only a `LocationType.Zone` may be one. The dependency probe therefore blocks retiring a zone with
  locations inside it, and `MasterDataReferenceRepointer` moves those children on a merge off the
  same declaration.
  ⚠️ **Two cycle paths, not one.** `LocationAncestry.WouldCycleAsync` guards re-parenting; the
  merge guards separately, because the repoint moves every child of the loser onto the survivor and
  a survivor that sat _inside_ the loser becomes its own parent. The merge handler clears the link
  before calling `MergeAsync`, so the save inside the merge transaction writes over the raw-SQL
  repoint.
  ⚠️ A zone has **no address** - it is a grouping, not a place. The validator refuses one and both
  handlers drop it, because a zone carrying one of its members' addresses is how a zone quietly
  becomes a duplicate delivery point.
  **Client delivery locations (outlets)** are Locations with a `CustomerId` (null = shared site),
  `DefaultCutoffTime` and `ZoneRadiusMeters` (for GPS arrival). `Load.OriginSiteId` /
  `DestinationSiteId` link a load to them; the load keeps its own address copy. `LoadSiteRules`
  refuses another customer's site, a zone, or an inactive site on a new link, and the Location
  policy declares both load columns so a merge moves loads to the survivor. UI: `app-search-location`
  - `app-save-site-dialog` on the load form, a Delivery Locations tab on customer-details. Seeded by
    `OutletSeeder` from `SeedData/ph-outlets.json` (1,920 outlets merged from Monday's 2026 CCL DAILY
    DELIVERY MONITORING by `Documents\monday-export\scripts\outlets.py`).

Shared rather than copied a fourth time: `BranchNameLookup` (the batched branch-name resolve every
`IBranchScoped` list needs - reading `record.Branch` in a projection loop is a query per row) and
`MasterDataFailure` (the 409 mapping for `DuplicateMasterDataRecord` / `MasterDataRecordInUse`; a
controller that quietly returned 400 would break that dialog for its entity alone).

- Domain: `Domain/Entities/MasterData/` (`Vessel`, `Commodity`, `Location`),
  `Domain.Primitives/Enums/MasterData/` (`VesselType`, `LocationType`)
- Application: `Modules/MasterData/Commands/{Vessel,Commodity,Location}/` (create, update, delete,
  restore, merge, row importer), `Queries/{Vessel,Commodity,Location}/`,
  `Modules/MasterData/Common/BranchNameLookup.cs`, `Common/LocationAncestry.cs`
- Infrastructure: `Persistence/Configurations/MasterData/` (`VesselConfiguration`,
  `CommodityConfiguration`, `LocationConfiguration`), migration `AddReferenceListsMasterData`
  (tables plus the hand-written trigram and `upper(btrim(...))` probe indexes EF cannot generate)
- API/UI: `Controllers/MasterData/{Vessel,Commodity,Location}Controller.cs`,
  `Controllers/MasterData/MasterDataFailure.cs`,
  `tms-portal/pages/master-data/{vessel,commodity,location}/`,
  nav in `shared/layout/sidebar/nav/master-data.nav.ts`
- Permission: `Permission.MasterData.{View,Manage}`
- Tests: `Trackwise.Application.Tests/MasterData/` (`{Vessel,Commodity,Location}CommandValidatorTests`,
  `{Vessel,Commodity,Location}RowImporterTests`)

### Fleet reference lists (Trailer, Helper)

The "boring" half of Fleet (`MASTER_DATA_PLAN.md` §5, "Why step 8 split") - both brand-new tables,
both `IBranchScoped`, both importable, both taking the vessel/location shape unchanged on
`MasterDataGroup.FleetAndPersonnel`. Promoting `Truck` and `Employee` themselves (the higher-risk
half - see next section) was deferred to its own later steps, not built here.

- **Trailer** - `Vin` is the module's second "second exact rule" after Vessel's IMO number (and
  ContainerType's ISO size-type code): globally unique, never reassigned, matched exactly alongside
  the fuzzy name rule, nullable and unique only among live rows where present. A merge where both
  sides carry different VINs is refused outright, the same guard Vessel's IMO rule uses.
- **Helper** - deliberately **not** built on `Employee`. Employee shares its primary key with the
  master-database `User` and carries payroll/Stripe Connect/invitation machinery a helper does not
  need; giving Helper that shape would import the exact entanglement that makes promoting `Employee`
  its own step. Helper stays a standalone record: identity, optional address, phone number.
- Neither declares a dependency yet, for Vessel's reason: nothing points at a trailer or a helper by
  foreign key until Truck's and Employee's own promotions wire assignment through these tables.

- Domain: `Domain/Entities/MasterData/` (`Trailer`, `Helper`),
  `Domain.Primitives/Enums/MasterData/TrailerType.cs`
- Application: `Modules/MasterData/Commands/{Trailer,Helper}/` (create, update, delete, restore,
  merge, row importer), `Queries/{Trailer,Helper}/`
- Infrastructure: `Persistence/Configurations/MasterData/` (`TrailerConfiguration`,
  `HelperConfiguration`), migration `AddFleetReferenceLists`
- API/UI: `Controllers/MasterData/{Trailer,Helper}Controller.cs`,
  `tms-portal/pages/master-data/{trailer,helper}/`, nav in
  `shared/layout/sidebar/nav/master-data.nav.ts` (added to the existing flat item list - **not** a
  new "Fleet" section, which would collide with the operational `fleet.nav.ts`)
- Permission: `Permission.MasterData.{View,Manage}`
- Tests: `Trackwise.Application.Tests/MasterData/` (`{Trailer,Helper}CommandValidatorTests`,
  `{Trailer,Helper}RowImporterTests`)

### Vehicle papers (VehiclePermit) and the vehicle register

A truck's or trailer's papers with expiry dates - LTO registration (OR/CR), LTFRB franchise,
insurance, NMIS accreditation - replacing the renewal columns on Monday's Admin > SKYBEST VEHICLES
board. Modeled on `DriverLicense`: a record with dates, not a file.

- **A renewal is a new record, never an edit.** The paper that counts is the latest-expiring one per
  vehicle and type (`VehiclePermitRules.CurrentIds`); older ones show as "Superseded" history.
- **Exactly one vehicle**: nullable `TruckId` xor `TrailerId`, guarded by the
  `ck_vehicle_permits_one_vehicle` check constraint and `ForTruck` / `ForTrailer`.
- **Reminders are one digest per tenant per day** (60/30/7 days, then on expiry), saved to the
  notifications inbox through `INotificationService`, not a live-only broadcast - the licence
  reminder's broadcast is lost when nobody is online. `SetExpiry` re-arms the thresholds. Sold or
  inactive vehicles are skipped. Not feature-gated.
- **Register fields on Truck** live in the partial `Truck.Register.cs`: `AssetCode` (R230325),
  `ConductionSticker` (the pre-plate LTO sticker), `EngineNumber`, `AcquiredOn`, `Supplier`,
  `Ownership` (null = not confirmed). `TruckType.WingVan` added; tractor heads are `ContainerTruck`.
  Create/update share `ITruckFields` + `TruckValidatorBase`.

- Domain: `Entities/Compliance/VehiclePermit.cs`, `Domain.Primitives/Enums/Compliance/VehiclePermitType.cs`,
  `Entities/Truck.Register.cs`, `Enums/Truck/TruckOwnership.cs`
- Application: `Modules/Compliance/VehiclePermits/` (`Commands/{Create,Update,Delete}VehiclePermit`,
  `Queries/GetVehiclePermits` - one vehicle with history, or the fleet's current papers of active
  vehicles; `VehiclePermitRules`, `VehicleLookup`, `Services/VehiclePermitReminderService`)
- Infrastructure: `Persistence/Configurations/Compliance/VehiclePermitEntityConfiguration.cs`,
  migrations `AddTruckRegisterFields`, `AddVehiclePermits`
- API/UI: `VehiclePermitController` (`/vehicle-permits`), `tms-portal/pages/vehicle-papers/` (fleet
  renewal queue, nav in `fleet.nav.ts`), `shared/components/fleet/` (`vehicle-papers` list on the
  truck details Papers tab and the trailer edit page, `vehicle-permit-dialog`, `permit-expiry`)
- Jobs: `VehiclePermitReminderJob` (`vehicle-permit-reminders`, daily 22:00 UTC)
- Permission: `Permission.Truck.{View,Manage}`
- Seed: `ph` tenant's `SeedData/ph-fleet.json` (`Ph:Fleet` trucks, `Ph:Trailers`, each with
  `Papers`), generated by `Documents\monday-export\scripts\fleet.py` from SKYBEST VEHICLES merged with
  the Cold Chain roster. `FleetSeeder` fills blank register fields on existing trucks (never
  overwrites); `TrailerSeeder` seeds real trailers instead of placeholders; `VehiclePermitSeeder`.
  The board's LTO "expiry" is the 2026 renewal's due date: a unit marked DONE gets the old paper
  plus a renewal expiring a year later (rule in `fleet.py`'s docstring).
- Tests: `Trackwise.Application.Tests/Compliance/VehiclePermitRulesTests.cs`

### Genset

A cold-chain equipment register, built the same "brand-new table, no dependents yet" way as
Trailer/Helper (see `COLD_CHAIN_LOGISTICS_CONTEXT.md` §3). A genset is a scarce shared physical
resource that optionally attaches to a reefer container booking - never booked standalone - so this
entity is just the unit registry (code, status, serial number); the reefer-workfile attachment and
its billing shape (itemized per-hour vs. flat profit-share) live on `Workfile` instead, not here.

- **SerialNumber** plays `Vin`'s role: nullable, upper-trimmed, unique only among live rows, and a
  merge where both sides carry different serial numbers is refused outright - same guard shape as
  Trailer's VIN rule.
- `Status` (`Available`/`InUse`/`UnderMaintenance`/`OutOfService`) is master-data metadata only here;
  nothing yet enforces "only an `Available` unit can be attached to a booking" - that belongs to
  whichever Workfile-side command does the attaching, once built.
- Declares no dependency yet, for Trailer's reason: nothing points at a genset by foreign key until
  the Workfile-side attachment references it.

- Domain: `Domain/Entities/MasterData/Genset.cs`, `Domain.Primitives/Enums/Genset/GensetStatus.cs`
- Application: `Modules/MasterData/Commands/Genset/` (create, update, delete, restore, merge, row
  importer), `Queries/Genset/`
- Infrastructure: `Persistence/Configurations/MasterData/GensetConfiguration.cs`
- API/UI: `Controllers/MasterData/GensetController.cs`, `tms-portal/pages/master-data/genset/`, nav
  in `shared/layout/sidebar/nav/master-data.nav.ts`
- Permission: `Permission.MasterData.{View,Manage}`

### Fleet promotion (Truck, Employee)

The higher-risk half of Fleet: two **existing, heavily-referenced** entities promoted in place
(`Truck` ~59 Application-layer call sites, `Employee` adds live-auth risk on top), not new tables.
`Terminal` (Ports) is the template; full rationale and what the promotion found and fixed -
including a real `DuplicateProbe` bug that reached production code before a live browser pass caught
it - is in `MASTER_DATA_PLAN.md` §6/§8, not repeated here.

- **`Truck.Number` is `IMasterDataRecord.Code`, forwarded rather than renamed.** The DB column stayed
  `number`; only the C# interface member forwards to it. `Vin` is the module's usual second exact
  rule. Old `Modules/Operations/Trucks/Commands/*` (Create/Update/Delete) deleted; `Queries/
{GetTruck,GetTrucks}` kept as the trimmed read-only surface dispatch/pickers use unchanged.
- **`Employee.Id` is the same GUID as a live `User.Id`.** `DeleteEmployeeHandler` and
  `MergeEmployeesHandler` both check the master-DB `User` row first and require
  `ConfirmedDespiteDependents` + a reason if it's still live - the guard against a data-hygiene
  operation silently deauthenticating someone mid-session. `GetCurrentUserPermissionsHandler` /
  `UserCustomClaimsFactory` needed **no** code change: a soft-deleted Employee already resolved to
  `null` there (same outcome as the old hard delete), which is already "no tenant access."
  Old `Modules/IdentityAccess/Employees/Commands/{Create,Update,Delete}Employee` deleted; `Queries/*`
  kept as the trimmed read-only surface (dispatch, reports, the driver mobile app).
- Both extend their **existing, richer** Angular pages in place rather than a parallel
  `pages/master-data/{truck,employee}/` tree - `pages/trucks/` (routed, `truck-edit`) and
  `pages/employees/` (dialog-based, `employee-edit-dialog`) respectively, following Port's "fully
  replace the old nav entry" pattern, not a duplicate one.
- Restore lives on the **list**, not the edit page (mirrors `PortsTable`): an "Include deleted"
  toggle plus a row-level action, since a deleted record has no reachable edit form.

- Domain: `Domain/Entities/{Truck,Employee}.cs`
- Application: `Modules/MasterData/Commands/Truck/{CreateTruck,UpdateTruck,DeleteTruck,RestoreTruck,
MergeTrucks}/`, `Queries/Truck/{GetTrucks,GetTruckById}/`, `Commands/Truck/ImportTrucks/`,
  `Modules/MasterData/Commands/Employee/{CreateEmployee,UpdateEmployee,DeleteEmployee,
RestoreEmployee,MergeEmployees}/`, `Queries/Employee/{GetEmployees,GetEmployeeById}/`,
  `Modules/Operations/TruckRefResolver.cs`, `Modules/IdentityAccess/Employees/
EmployeeRefResolver.cs` (batched display-name resolvers, modeled on `TerminalRefResolver`)
- Infrastructure: `Persistence/Configurations/{TruckEntityConfiguration,
EmployeeEntityConfiguration}.cs`, migrations `AddTrucksMasterData`, `AddEmployeesMasterData`
- API/UI: `Controllers/MasterData/{TruckController,EmployeeController}.cs` (new, write path) vs the
  existing `Controllers/{TruckController,EmployeeController}.cs` (trimmed read-only), `tms-portal/
pages/trucks/`, `tms-portal/pages/employees/`, nav in `master-data.nav.ts` (both added) /
  `fleet.nav.ts` and `people.nav.ts` (both removed, Port-style full replacement)
- Permission: `Permission.MasterData.{View,Manage}` for the write path; the old
  `Permission.Truck.View` / `Permission.Employee.View` / `Permission.Driver.View` reads stay on the
  trimmed controllers and the driver app, untouched
- Tests: `Trackwise.Application.Tests/MasterData/` (`TruckCommandValidatorTests`,
  `TruckDisplayLookupTests`, `EmployeeCommandValidatorTests`, `EmployeeDisplayLookupTests`,
  `EmployeePermissionResolutionTests`, `UpdateEmployeeHandlerTests`),
  `Trackwise.Infrastructure.Persistence.Tests/MasterData/` (`TruckSoftDeleteTests`,
  `EmployeeSoftDeleteTests`)

### Container types and currencies

The two lists that are not the ports shape. Both are **global** - not `IBranchScoped` - because ISO
6346 and ISO 4217 do not vary by office, so neither appears in `Branch.DependencyRules` and the
reciprocal policy test permits that only while `BranchScoped` is false.

- **`ContainerType` replaces two enums, and is the pattern for any list that has to.**
  `ContainerIsoType` (on `containers`) and `EquipmentType` (on `bookings` and `tariffs`) described
  the same boxes in two vocabularies; the table is their union, and both retired vocabularies live
  on as `MasterDataXref` rows so a legacy value still resolves. `IsoSizeTypeCode` is the exact
  duplicate rule that does the real work (the vessel-IMO role here), nullable and unique only among
  live rows where present. `ContainerTypeLookup` batches the code/name resolve for the DTOs;
  `ContainerTypeIsoLookup` resolves a legacy ISO value through the xref.
  ⚠️ The migration `AddContainerTypesAndCurrencies` is **ordered, not scaffolded**: seed,
  cross-reference, backfill the three FKs through the xref, verify, then drop the enum columns. EF's
  own ordering drops first and defaults the new column to the zero GUID.
  ⚠️ `EquipmentType` still exists as an enum - the load board speaks it (`LoadBoardListing`,
  `PostedTruck`) and that is a vendor vocabulary, not master data. It is gone only from `Booking`
  and `Tariff`.
- **`Currency` is the list, not the rates.** `EffectiveDated` stays false on the policy: FX rates
  are step 9's machinery. The code is the one master code never allocated from `NumberSeries` -
  ISO 4217 assigns it - and `NumericCode` is a second exact rule. `CurrencyNumericLookup` resolves
  it.
  ⚠️ Its `DependencyRules` is empty and that is **not** "nothing depends on a currency". Every
  `Money` holds a three-letter string, and a dependency rule matches a foreign-key column against
  the record's id, so the probe cannot see the invoices that do. Do not read the empty list as a
  clearance.

Consumers moved with the columns: `ContainerDto`, `BookingDto` and `TariffDto` carry
`ContainerTypeId` plus the resolved `ContainerTypeCode` / `ContainerTypeName` (batched, never off
the navigation property), and `GetContainersQuery` filters by `ContainerTypeId`. In the portal the
label no longer exists client-side - `ContainerTypeOptionsService` (`tms-portal/core/services/`)
loads the active list once for the container, tariff and quotation-conversion forms, and
`include(id)` adds a retired type back so an edit form opened on one does not silently rewrite it.

- Domain: `Domain/Entities/MasterData/` (`ContainerType`, `Currency`),
  `Domain.Primitives/Enums/MasterData/ContainerTypeCategory.cs`
- Application: `Modules/MasterData/Commands/{ContainerType,Currency}/`,
  `Queries/{ContainerType,Currency}/`, `Modules/MasterData/Common/ContainerTypeLookup.cs`,
  `Common/ContainerTypeIsoLookup.cs`, `Common/CurrencyNumericLookup.cs`
- Infrastructure: `Persistence/Configurations/MasterData/` (`ContainerTypeConfiguration`,
  `CurrencyConfiguration`), migration `AddContainerTypesAndCurrencies`
- API/UI: `Controllers/MasterData/{ContainerType,Currency}Controller.cs`,
  `tms-portal/pages/master-data/{container-type,currency}/`,
  `tms-portal/core/services/container-type-options.service.ts`, nav in
  `shared/layout/sidebar/nav/master-data.nav.ts`
- Permission: `Permission.MasterData.{View,Manage}`
- Tests: `Trackwise.Application.Tests/MasterData/` (`{ContainerType,Currency}CommandValidatorTests`,
  `{ContainerType,Currency}RowImporterTests`), `Operations/TariffTests.cs`

### Partners — maker-checker

`BusinessPartner` (the existing freight-forwarding entity) is the first `IMasterDataApprovable`:
a record that cannot go active on one person's say-so. Everything below is new; the reference lists
above never needed it because `Approval = MasterDataApprovalMode.None` on their policies.

- **`IMasterDataApprovable.ApprovalState`** is stored, not derived - the one field
  `MasterDataState`'s "derived, not stored" rule (MASTER_DATA_PLAN.md §4) names as its own exception.
  `MasterDataApprovalRequest` (`Domain/Entities/MasterData/`) is the history table: one row per
  submission, so a record submitted, rejected and resubmitted keeps every round rather than only the
  last. `IsOpen` (`Decision is null`) is the approver's queue filter.
- **`MasterDataApprovalService`** (`Application/Modules/MasterData/Common/`) is the entity-agnostic
  half - `SubmitAsync` / `ApproveAsync` / `RejectAsync` - shared by every future approvable entity.
  `ApproveAsync` is the only place `IsActive` is set true; `RejectAsync` returns the record to
  `Draft` rather than a state of its own. The self-approval rule
  (`request.SubmittedBy == reviewer` refused with `ErrorCodes.MasterDataSelfApproval`) is checked
  against the request, not a column on the record, which is why the interface carries no
  `SubmittedBy`.
- **Partner's own commands** (`Modules/MasterData/Commands/Partner/`): `CreatePartner` starts every
  record `Draft`/`IsActive = false` regardless of what the request asks; `UpdatePartner` refuses a
  caller trying to set `IsActive` directly unless the record is already `Approved`; `SubmitPartner`
  refuses a partner with no roles (nothing for an approver to accept it as); `ApprovePartner`
  re-checks the code for a live collision immediately before flipping the flag, because the unique
  index is filtered to live rows and two drafts can share a code until one of them goes live;
  `DeletePartner` withdraws any open request (marks it rejected with a system note) instead of
  leaving it pointing at nothing; `MergePartners` refuses to fold an approved partner into an
  unapproved survivor, and refuses outright when both sides carry different TINs.
- **`TaxId`** is the vessel-IMO role for this entity: digits-only via `NormalizeTaxId`, the
  strongest duplicate signal on the list because a company trades under multiple names on purpose.
- **`Permission.MasterData.Approve`** is new alongside `.View`/`.Manage`. No new `UserRoles` entry -
  `TenantRolePermissions` grants it to `Owner` and `Manager` (not `Dispatcher`, which keeps `.View`
  only). The self-approval rule is enforced at the record regardless of who holds the permission.
- **Angular**: `tms-portal/pages/master-data/partner/` (list, add, edit, the shared
  `partner-approval` component) and `tms-portal/pages/master-data/approvals/` (the cross-entity
  queue, oldest-open-request-first). Submit/approve/reject buttons are gated client-side on
  `Permission.MasterData.Approve` and enforced again server-side - the client gate is convenience,
  not the guarantee.

- Domain: `Domain/Entities/FreightForwarding/BusinessPartner.cs` (now `IMasterDataApprovable`),
  `Domain/Entities/MasterData/MasterDataApprovalRequest.cs`, `Domain/Core/IMasterDataApprovable.cs`,
  `Domain.Primitives/Enums/MasterData/MasterDataApprovalState.cs`
- Application: `Modules/MasterData/Commands/Partner/` (Create, Update, Delete, Restore, Submit,
  Approve, Reject, Merge, Import), `Queries/Partner/`, `Queries/Approvals/GetMasterDataApprovals/`,
  `Modules/MasterData/Common/MasterDataApprovalService.cs`
- Infrastructure: `Persistence/Configurations/MasterData/MasterDataApprovalRequestConfiguration.cs`,
  migration `AddPartnersMasterData`
- API/UI: `Controllers/MasterData/PartnerController.cs`,
  `Controllers/MasterData/MasterDataApprovalController.cs` (the cross-entity queue endpoint),
  `tms-portal/pages/master-data/{partner,approvals}/`, nav in
  `shared/layout/sidebar/nav/master-data.nav.ts`
- Permission: `Permission.MasterData.{View,Manage,Approve}`
- Tests: `Trackwise.Application.Tests/MasterData/` (`PartnerCommandValidatorTests`,
  `PartnerRowImporterTests`, `MasterDataApprovalServiceTests`, `MasterDataMergeServiceTests`)

### Duplicate probe and entity policy

The stage that makes this a master-data system rather than a CRUD screen: a write is checked
against existing records **before** it is saved.

- **`MasterDataPolicies`** (`Application.Abstractions/Modules/MasterData/`) is the governance table -
  approval mode, duplicate rules, effective dating, branch scoping, thresholds. Adding an entity
  means adding an entry, **never** an `if (entityType == ...)` downstream. Its table and column
  names are interpolated into the probe's SQL, so they must stay compile-time constants.
- **`MasterDataWriteBehaviour`** runs the probe for any command implementing
  `IMasterDataWriteCommand`, registered after `ValidationBehaviour`. It short-circuits with
  `ErrorCodes.DuplicateMasterDataRecord`; the controller maps that to **409**, everything else 400.
  Overriding needs `ConfirmedDistinct` **and** a reason - the validator enforces the reason and the
  behaviour logs the override.
- **`DuplicateProbe`** uses PostgreSQL `similarity()` (pg*trgm) for names and equality for
  identifiers, over a raw command - `similarity()` has no LINQ translation, and change-tracking
  proxies reject the sealed projection a typed `SqlQuery<T>` would need (the same trap that bit
  `MasterDataCodeGenerator`).
  ⚠️ It deliberately **bypasses the soft-delete query filter**: the most common duplicate is one
  somebody deleted last year.
  ⚠️ The trigram index expression must match the probe predicate exactly
  (`lower(btrim(col))`) or the index is unused and every probe is a sequential scan.
  ⚠️ Matching the expression is **not sufficient**: `similarity(a, b) >= threshold` is a per-row
  function call no index can answer, so the probe also carries an index-usable `%` pre-filter ORed
  across its rules, and each exact rule needs its own `upper(btrim(col))` expression index
  (`ix*\*\_code_probe`) or the planner has no `BitmapOr`to build and falls back to a scan anyway.
The pre-filter is a superset of the scored result only while every soft threshold clears
PostgreSQL's`pg_trgm.similarity_threshold`(0.3) -`MasterDataPolicyTests` pins that.
- Read-only endpoint `POST /master-data/duplicates/probe` so the UI can call it while the steward
  types.

- Application: `Behaviours/MasterDataWriteBehaviour.cs`,
  `Modules/MasterData/Common/` (`IMasterDataWriteCommand`, `DuplicateRecordMessage`),
  `Modules/MasterData/Queries/Duplicates/ProbeDuplicates/`
- Infrastructure: `Persistence/Services/MasterData/DuplicateProbe.cs`,
  migration `AddTrigramDuplicateProbeSupport` (enables pg_trgm)
- Tests: `Trackwise.Application.Tests/MasterData/`

### Dependency probe on retirement

The mirror image of the duplicate probe: that one stops a record arriving, this one stops a record
leaving while other records still point at it.

- **`DependencyRule`** entries on the policy declare where to look - table, FK column, a label the
  steward reads, and an optional `LiveCondition` narrowing the count to rows that still matter.
  `DependencyRules` is **required** on every policy: an empty list is a valid answer, but it has to
  be written down, because forgetting fails silently as "nothing depends on this".
- **`MasterDataDeactivateBehaviour`** runs it for any command implementing
  `IMasterDataDeactivateCommand`, registered after `MasterDataWriteBehaviour`. It short-circuits
  with `ErrorCodes.MasterDataRecordInUse`, which `BranchController.Failure` maps to **409**.
  Overriding needs `ConfirmedDespiteDependents` **and** a reason, exactly as the duplicate gate does.
- **Deactivate and delete are probed differently.** A rule's `AppliesTo` says which it objects to,
  and the refusal message differs: deactivating something in use is often correct (an office closes,
  its history stays), while deleting means "created by mistake", which being in use contradicts.
- ⚠️ **The probe only fires on a genuine transition.** Its SQL hangs the counts off the record's own
  row with `AND is_active AND deleted_at IS NULL` (deactivate) or `AND deleted_at IS NULL` (delete),
  so no row comes back when the record is already retired. Without that, editing the name of an
  already-inactive record with dependents would be refused.
- A rule with `Blocks: false` is advisory - counted and reported, never an obstacle. A probe that
  refuses everything gets overridden by reflex.
- `Branch` declares one rule - the ports scoped to it - and
  `MasterDataPolicyTests.EveryBranchScopedEntity_IsDeclaredAsABranchDependency` fails if a future
  `IBranchScoped` entity lands without its own.

- Application: `Behaviours/MasterDataDeactivateBehaviour.cs`,
  `Modules/MasterData/Common/` (`IMasterDataDeactivateCommand`, `DependentRecordMessage`)
- Abstractions: `Modules/MasterData/` (`IDependencyProbe`, `DependencyRule`),
  `Domain.Primitives/Enums/MasterData/MasterDataDeactivation.cs`
- Infrastructure: `Persistence/Services/MasterData/DependencyProbe.cs` (one round trip: correlated
  `count(*)` subqueries per rule, counted not loaded)

### Merge and cross-reference forwarding

Two branches create the same record; a steward folds one into the other and **every historical
reference still resolves**.

- `MasterDataMergeService` (`Application/Modules/MasterData/Common/`) owns the entity-agnostic half:
  repointing `MasterDataXref` rows at the survivor, writing the loser's own id as a forwarding
  xref, snapshotting into `MasterDataMergeHistory`, tombstoning the loser. Field decisions stay in
  the entity's handler, where they are type-safe.
  ⚠️ It does not open its own transaction - it asserts `tenantUow.HasActiveTransaction` and throws a
  named error otherwise. Every merge handler (`MergeBranches`, `MergePorts`, `MergePartners`) calls
  it from inside its own `ExecuteInTransactionAsync`, because the survivor has to be loaded and
  edited before the repoint runs and a retry replays against a cleared change tracker.
- **A merge on an approvable entity has to defend the approval invariant too, not just referential
  integrity.** `MergePartnersHandler` refuses to fold an approved record into an unapproved
  survivor - the record would go live under the survivor's references with nobody having reviewed
  it - and `ApprovalState` is not one of the fields a steward can choose to take from the loser.
- `GetBranchByIdHandler` follows the chain: an id merged away resolves to the survivor with
  `ForwardedFromId` set, so a client can say "that record is now this one" rather than silently
  showing something else. Chains resolve transitively, capped at 10 hops so a cycle cannot hang a
  read.
- **`Merged` is terminal** - re-merging a tombstone is refused, or the chain forks and a lookup
  could resolve to either end.
- A merge also soft-deletes the loser so it leaves the lists. ⚠️ That is why
  `ChangeLogSaveChangesInterceptor` tests `MergedIntoId` **before** `DeletedAt`: the other order
  records every merge as an ordinary deletion (it did, until it was caught end-to-end).
- Restore's code-clash message points at merge rather than dead-ending.

- Application: `Modules/MasterData/Commands/Branch/MergeBranches/`,
  `Modules/MasterData/Common/MasterDataMergeService.cs`
- API: `POST /master-data/branches/{id}/merge` (route id is the survivor)

### Bulk import with dry run

A steward loads a legacy list from a file and is told what it would do before it does it. The two
properties everything else serves: a **dry run is the real import rolled back**, and an import is
**all-or-nothing**.

- **`MasterDataImporter`** (`Application/Modules/MasterData/Common/Import/`) owns everything that is
  the same for every entity: parsing, the header check, the transaction, the duplicate retry and the
  report. Each row is sent as that entity's own create command **through `IMediator`**, so it
  inherits validation, the duplicate probe and the change log. An importer that wrote entities
  directly would be a second write path, and a bypassable guarantee is none.
- ⚠️ **Dry run runs the handlers.** It is a real transaction that is rolled back, not a validation
  pass. A cheaper rehearsal would pass files whose commit then fails on a code clash or a missing
  branch - which is worse than no rehearsal, because it is believed.
- **`IMasterDataRowImporter`** is all an entity supplies: its columns and a row -> command binding.
  It resolves and rejects (an unknown branch code, a word that is not a terminal type); it must
  **not** restate field rules, or the file and the steward's form drift into disagreeing about what
  is legal. Registered as a set in `Registrar`, picked by entity type - nine today: ports,
  vessels, commodities, locations, container types, currencies, partners, trailers and helpers.
- ⚠️ **Soft failures are collected, database failures stop the run.** Validation and handler
  rejections never touch the database, so the pass continues and one dry run names every offending
  row. A `DbUpdateException` cannot be collected - PostgreSQL marks the transaction aborted and
  every later statement fails for a reason unrelated to its own row - so the report says where it
  stopped instead of listing noise.
- **Intra-batch duplicates need no special case.** Rows are written sequentially inside one
  transaction, so row N's probe already sees rows 1..N-1. A file that duplicates itself fails the
  same way a second manual entry would. ⚠️ This rests on `DuplicateProbe` raising a raw `DbCommand`
  without setting `Transaction`: Npgsql ignores that property and runs every command in the
  connection's current transaction. `SqlClient` **throws** instead - worth knowing before the nSFS
  ETL points the same pattern at SQL Server.
- **`DelimitedTable`** is hand-rolled for one reason: every row carries the physical line it started
  on, including rows whose quoted fields span lines. That number is what the report points at, and
  a row index would be off by one from the steward's spreadsheet. It sniffs `,` `	` `;` `|` from the
  header and matches headers case- and punctuation-insensitively.
- Overriding the duplicate probe for a whole file needs `acceptDuplicates` **and** a reason, and
  every row it lets through comes back as a warning in the report - a count alone would not say
  which rows it covered.
- The template endpoint is built from the same `Columns` declaration the import reads, so the
  headers a steward is told to use cannot drift from the ones accepted.

- Abstractions: `Modules/MasterData/` (`IMasterDataImporter`, `IMasterDataRowImporter`,
  `MasterDataImport.cs` - request, row, issue, report, column)
- Application: `Modules/MasterData/Common/Import/` (`MasterDataImporter`, `DelimitedTable`),
  `Modules/MasterData/Commands/Import/ImportMasterData/`, and one `*RowImporter` per importable
  entity under
  `Modules/MasterData/Commands/{Port,Vessel,Commodity,Location,ContainerType,Currency,Partner,Trailer,Helper}/Import*/`
- API: `POST /master-data/imports/{entityType}` (multipart; **`commit=true` writes, absent = dry
  run** - the unrecoverable direction is never the default),
  `GET /master-data/imports/{entityType}/template`
- Tests: `Trackwise.Application.Tests/MasterData/` (`MasterDataImporterTests`,
  `DelimitedTableTests`, and a `*RowImporterTests` per importable entity)

## Tests

All under `test/` (singular):

- `Trackwise.Application.Tests/` - application handlers, services
- `Trackwise.Architecture.Tests/` - layering/dependency boundaries; discovers projects, never hard-code a list
- `Trackwise.Infrastructure.AI.Tests/` - AI agent, tools, prompts, pricing (quota lives in `Trackwise.Infrastructure.Persistence.Tests/`, next to `AIQuotaService`)
- `Trackwise.Infrastructure.{Documents,Payments,Persistence,Routing,Tax,Vin}.Tests/` - per-module

## Updating this map

Add a `### Feature` entry when a top-level feature lands. Don't add sub-features (e.g. _PaymentLink expiration_ belongs under Payment links - the entry already points to the right folders). If a path moves, update the line, don't leave a stale one. Only list the layers a feature actually touches.
