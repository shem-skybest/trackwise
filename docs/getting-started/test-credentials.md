# Test Credentials

Accounts created by the database migrator (`src/Presentation/Trackwise.DbMigrator`). Every password is `Test12345#`.

## Live Demo

A live demo is available at [https://tms.trackwise.app](https://tms.trackwise.app)

> **Warning**: The demo uses test Stripe keys. Do not enter real payment information.

## Super Admin

Runs the Admin Portal and is not attached to any tenant. Its credentials come from the `SuperAdmin` section of the migrator's `appsettings.json` (override with `SuperAdmin__Email` / `SuperAdmin__Password`), so a deployment can seed a different one.

| Email                  | Password   | Application  |
| ---------------------- | ---------- | ------------ |
| `admin@skybest.com.ph` | Test12345# | Admin Portal |

## Demo Tenants

Three tenants are seeded. The tenant slug is what the client sends in the `X-Tenant` header, and it is also the `{tenant}` in the `{tenant}_trackwise` database name.

| Slug   | Company                       | Region | Database              | Operating mode | Shape                                          |
| ------ | ----------------------------- | ------ | --------------------- | -------------- | ---------------------------------------------- |
| `ph`   | Skybest Logistics Corporation | PH     | `skybest_trackwise`   | Fleet          | 10 employees, 5 trucks, 5 customers, 100 loads |
| `eu`   | Bestchill Incorporated        | EU     | `bestchill_trackwise` | Fleet          | 10 employees, 5 trucks, 5 customers, 100 loads |
| `solo` | Rodriguez Trucking LLC        | US     | `solo_trackwise`      | Owner-operator | 1 employee, 1 truck, 2 customers, 12 loads     |

The PH tenant bills in PHP, uses metric units and the Asia/Manila timezone. The EU tenant bills in EUR, uses metric units and the Europe/Berlin timezone. The `solo` tenant runs in owner-operator mode, so the TMS portal hides employees, payroll, timesheets and messaging for it.

### PH tenant (`ph`)

| Role       | Email                        | Application       |
| ---------- | ---------------------------- | ----------------- |
| Owner      | `owner@skybest.com.ph`       | TMS Portal        |
| Manager    | `manager1@skybest.com.ph`    | TMS Portal        |
| Dispatcher | `dispatcher1@skybest.com.ph` | TMS Portal        |
| Dispatcher | `dispatcher2@skybest.com.ph` | TMS Portal        |
| Dispatcher | `dispatcher3@skybest.com.ph` | TMS Portal        |
| Driver     | `driver1@skybest.com.ph`     | Driver Mobile App |
| Driver     | `driver2@skybest.com.ph`     | Driver Mobile App |
| Driver     | `driver3@skybest.com.ph`     | Driver Mobile App |
| Driver     | `driver4@skybest.com.ph`     | Driver Mobile App |
| Driver     | `driver5@skybest.com.ph`     | Driver Mobile App |
| Customer   | `customer1@skybest.com.ph`   | Customer Portal   |
| Customer   | `customer2@skybest.com.ph`   | Customer Portal   |

### EU tenant (`eu`)

Same shape, `eu_` prefixed. Note the manager has no number - it is `eu_manager@bestchill.com.ph`, not `eu_manager1@bestchill.com.ph`.

| Role       | Email                             | Application       |
| ---------- | --------------------------------- | ----------------- |
| Owner      | `eu_owner@bestchill.com.ph`       | TMS Portal        |
| Manager    | `eu_manager@bestchill.com.ph`     | TMS Portal        |
| Dispatcher | `eu_dispatcher1@bestchill.com.ph` | TMS Portal        |
| Dispatcher | `eu_dispatcher2@bestchill.com.ph` | TMS Portal        |
| Dispatcher | `eu_dispatcher3@bestchill.com.ph` | TMS Portal        |
| Driver     | `eu_driver1@bestchill.com.ph`     | Driver Mobile App |
| Driver     | `eu_driver2@bestchill.com.ph`     | Driver Mobile App |
| Driver     | `eu_driver3@bestchill.com.ph`     | Driver Mobile App |
| Driver     | `eu_driver4@bestchill.com.ph`     | Driver Mobile App |
| Driver     | `eu_driver5@bestchill.com.ph`     | Driver Mobile App |
| Customer   | `eu_customer1@bestchill.com.ph`   | Customer Portal   |
| Customer   | `eu_customer2@bestchill.com.ph`   | Customer Portal   |

### Solo tenant (`solo`)

| Role     | Email                           | Application                    |
| -------- | ------------------------------- | ------------------------------ |
| Owner    | `solo@skybest.com.ph`           | TMS Portal + Driver Mobile App |
| Customer | `solo_customer1@skybest.com.ph` | Customer Portal                |

`solo@skybest.com.ph` is the tenant's only employee. They hold the Owner role and are the main driver of the tenant's single truck, so the same login works in both the TMS portal and the driver app. Owner carries `Permission.Driver.*`, which is what makes the driver app usable here.

> The Owner-to-driver-app path depends on `TenantRoleClaim` rows in each tenant database, not on the C# permission list. If a driver-app call 403s after a permission change, re-run the migrator so `TenantRoleSeeder` re-syncs them.

## Role Permissions

### Super Admin (Admin Portal only)

- Manage all tenants (companies)
- View system-wide statistics
- Manage subscriptions and billing
- Create and manage app admins

### Owner (TMS Portal, and the driver app when assigned to a truck)

- Full access to company data and company settings
- Manage employees, roles, and invitations
- Financials: invoices, payments, payroll, expenses, tax, accounting sync
- Everything Manager and Dispatcher can do

### Manager (TMS Portal)

- Loads, trips, trucks, customers, containers, terminals
- Invoices, payments, payroll, expenses
- ELD/HOS, DVIR review, safety, maintenance
- Manage employees and send invitations, but not company settings or roles

### Dispatcher (TMS Portal)

- Create and manage loads, assign drivers, manage trips
- Customers, containers, terminals, load board search/book/post
- View invoices, payments, trucks, ELD/HOS
- AI dispatch sessions and decisions

### Driver (Driver Mobile App)

- View assigned loads and trips, update load status
- Upload documents (POD, BOL) and share GPS location
- File and submit DVIRs (review/sign-off stays with Owner and Manager)
- Message dispatch

### Customer (Customer Portal)

- Track their own shipments, download documents, pay invoices

## Application Access Matrix

| Area                              | Super Admin | Owner | Manager | Dispatcher | Driver |
| --------------------------------- | :---------: | :---: | :-----: | :--------: | :----: |
| Tenant management (all companies) |      X      |       |         |            |        |
| Company settings                  |             |   X   |         |            |        |
| Employees & roles                 |             |   X   | Manage  |            |        |
| Loads & trips                     |             |   X   |    X    |     X      |  View  |
| Customers                         |             |   X   |    X    |     X      |        |
| Invoices & payments               |             |   X   |    X    |    View    |        |
| Payroll                           |             |   X   |    X    |            |        |
| Expenses                          |             |   X   |    X    |     X      |        |
| Load board                        |             |   X   |    X    |     X      |        |
| ELD / HOS                         |             |   X   |    X    |    View    |        |
| DVIR                              |             |   X   |    X    |            | Submit |
| Safety & maintenance              |             |   X   |    X    |            |        |
| AI dispatch                       |             |   X   |    X    |     X      |        |
| Analytics dashboard               |      X      |   X   |  View   |    View    |  View  |
| Messaging                         |             |   X   |    X    |     X      |   X    |

Source of truth: `src/Shared/Trackwise.Shared.Identity/Policies/TenantRolePermissions.cs`.

## Creating Additional Test Users

Via the Admin Portal (super admin) or the TMS Portal (owner or manager):

1. Navigate to the Employees section
2. Invite a user by email and assign a role
3. The invitee receives an email with a link to set their password

## Password Requirements

- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character

## Resetting Test Data

To reset to initial test data:

```bash
# Drop and recreate databases
psql -U postgres -c "DROP DATABASE IF EXISTS master_trackwise;"
psql -U postgres -c "DROP DATABASE IF EXISTS skybest_trackwise;"
psql -U postgres -c "DROP DATABASE IF EXISTS bestchill_trackwise;"
psql -U postgres -c "DROP DATABASE IF EXISTS solo_trackwise;"
psql -U postgres -c "CREATE DATABASE master_trackwise;"

# Re-run migrations and seeding (the migrator creates the tenant databases itself)
dotnet run --project src/Presentation/Trackwise.DbMigrator
```

Or with Docker:

```bash
docker compose -f deploy/docker-compose.dev.yml down -v
docker compose -f deploy/docker-compose.dev.yml up -d
```
