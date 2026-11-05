---
paths:
  - "src/Core/Trackwise.Domain/**/*.cs"
  - "src/Core/Trackwise.Application/**/*.cs"
  - "src/Infrastructure/**/*.cs"
---

# EF Persistence Traps

## New entities MUST be registered with `repository.AddAsync`

Entity ids are pre-generated (`Guid.NewGuid()` in the `Entity` base), so EF treats a new entity
that reaches the tracker only via a tracked parent's navigation collection as an existing row and
saves an UPDATE affecting 0 rows - a misleading `DbUpdateConcurrencyException`.

```csharp
var message = conversation.AddTextMessage(role, text);
await tenantUow.Repository<AgentMessage>().AddAsync(message, ct); // without this, save throws
await tenantUow.SaveChangesAsync(ct);
```

Exempt only when the parent itself is new and added as a graph (`LoadFactory` / `TripFactory` -
the cascade marks everything Added). `NavigationDiscoveryGuard` (attached to both DbContexts)
turns the mistake into an immediate, named error - fix the call site, don't remove the guard.

## Concurrency-token asymmetry

Tenant entities have no concurrency tokens - a `DbUpdateConcurrencyException` on tenant data means
a missing row (usually this trap), not a write conflict. Only master `User` / `AppRole` carry a
`ConcurrencyStamp` and can conflict for real.

## Never open a transaction yourself

`ConfigurePostgreSql` enables `EnableRetryOnFailure`, and the resulting
`NpgsqlRetryingExecutionStrategy` refuses any transaction the caller began. A hand-rolled
`Database.BeginTransactionAsync` compiles and passes every test that substitutes the unit of work,
then throws on its first `SaveChanges` against a real database:

> The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not support
> user-initiated transactions.

`IUnitOfWork` therefore exposes no `BeginTransactionAsync`. Use `ExecuteInTransactionAsync`, which
hands the whole unit to the execution strategy so a retry replays it as one piece:

```csharp
return await tenantUow.ExecuteInTransactionAsync(
    async token =>
    {
        var record = await tenantUow.Repository<Thing>().Query().FirstAsync(..., token); // inside
        record.Rename(req.Name);
        return Result<ThingDto>.Ok(record.ToDto());   // failure rolls back, success commits
    },
    ct);
```

Two rules the retry imposes:

- **Load inside the delegate.** Each attempt starts with a cleared change tracker, so an entity read
  before the call is detached from the second attempt on and its edits are silently dropped.
- **Build counters and accumulated state inside the delegate**, or a retry reports the abandoned
  attempt's work on top of this one's.

The `Result<T>` overload above commits on success and rolls back on failure. For a unit that decides
for itself - a dry run is the real work deliberately discarded - return
`TransactionOutcome<T>.Keep` / `.Discard`.
