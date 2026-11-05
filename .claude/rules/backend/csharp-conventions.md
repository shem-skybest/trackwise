---
paths:
  - "**/*.cs"
---

# C# Code Conventions

Style, naming casing, and nullability are enforced by `.editorconfig` and the analyzers - match the
surrounding file and let the build tell you. What follows is the part no tool checks.

## Non-obvious defaults

- Primary constructors for DI; one type per file matching the filename.
- `Async` suffix on async methods (**except** MediatR handlers). Always accept `CancellationToken`; never `.Result` / `.Wait()`.

## Domain Events

- Raise events in entity methods, not handlers. Handlers in `Trackwise.Application/Modules/{Module}/{Feature}/Events/`

## Naming

- Commands: `{Action}{Entity}Command` → Handlers: `{Command}Handler` (internal sealed)
- Queries: `Get{Entity}ByIdQuery`, `Get{Entities}Query`
- DTOs: `{Entity}Dto`, Mappers: `{Entity}Mapper`
- Private fields: `camelCase` (no `_` prefix) - enforced by `.editorconfig`
- Constants and static readonly: `PascalCase`

## Enum-named properties

A property may share its name with its enum type (`CreditType`, `GensetBillingType`) - it reads
naturally in a DTO or entity. But **inside that type**, referencing one of the enum's members by
bare name breaks: `GensetBillingType.Itemized` in an expression or pattern resolves `GensetBillingType`
to the _property_ first (instance members outrank type names in simple-name lookup), then fails to
find `.Itemized` on that property's type. This only surfaces where the enum is actually
pattern-matched or compared from inside the declaring type - a property that's just passed through
(like `CreditType` today) never hits it, which is why the trap sat unnoticed. If you need to write
`Property.Member` inside the class that declares `Property`, add a using-alias under a different
name (`using GensetBillingKind = Trackwise.Domain.Primitives.Enums.GensetBillingType;`) and reference
the alias, rather than fully-qualifying every call site or renaming the property.

## Validation

- Don't write a validator whose only rule is `Id NotEmpty` - the handler's not-found path covers it. For Create/Update pairs sharing ≥3 rules, use the shared-fields base-validator pattern (see Ifta TaxRates).

## Enum Display Names

Use `GetDescription()` from `Trackwise.Domain.Primitives.Enums.EnumExtensions` for display strings - it auto-humanizes enum names (`PickedUp` → "Picked Up"). Only use `[Description]` when humanization isn't enough: acronyms (`Eld` → "ELD / HOS"), special formatting (`OnDutyNotDriving` → "On Duty (Not Driving)"), domain-specific (`Driving11Hour` → "11-Hour Driving Limit").

## API Query Sort Syntax

Sort parameters use `-FieldName` for descending, plain `FieldName` for ascending. Do NOT use `"FieldName desc"` / `"FieldName asc"` - the API parser doesn't accept that form. Same syntax applies on every client (Angular, Kotlin driver app, MCP).
