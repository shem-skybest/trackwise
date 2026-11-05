## What and why

<!-- What does this change, and what problem does it solve? Someone reading this in three months should understand it without asking you. -->

## Database

- [ ] No migration in this PR
- [ ] Has a migration. What happens to rows that already exist? (e.g. a new NOT NULL column or unique index on a table that already has data, like Cold Chain bookings with no quotation)

<!-- If it has a migration, describe its effect on existing data here. -->

## How it was tested

<!-- Where did you run it (local, which tenant), what did you click or call, what did you check? "It builds" is not a test. -->

## Checklist

- [ ] `dotnet test` passes locally
- [ ] UI uses the shared `ui-*` components (no plain `<table>`, no hand-built section headers, no raw `hlm*`)
- [ ] No hardcoded colours (`text-amber-900`, `bg-white`, ...): theme tokens only
- [ ] Templates stay under ~300 lines, or the PR says why not
