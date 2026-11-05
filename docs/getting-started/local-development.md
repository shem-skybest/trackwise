# Local Development Setup

Step-by-step guide to run TrackWise without Docker.

## Step 1: Clone Repository

```bash
git clone https://github.com/shem-skybest/skybest-logistics-corporation.git
cd logistics-app
```

## Step 2: Install Angular Dependencies

```bash
cd src/Client/Trackwise.Angular
bun install
cd ../../..
```

## Step 3: Configure Database

### Create the Master Database

Only the master database has to exist up front - the migrator creates each tenant database itself:

```bash
psql -U postgres -c "CREATE DATABASE master_trackwise;"
```

The seeded demo tenants are `ph` (Skybest Logistics Corporation), `eu` (Bestchill Incorporated) and `solo`
(Rodriguez Trucking LLC, owner-operator mode), giving you `skybest_trackwise`, `bestchill_trackwise` and
`solo_trackwise`.

### Update Connection Strings

Edit `src/Presentation/Trackwise.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MasterDatabase": "Host=localhost;Port=5433;Database=master_trackwise;Username=postgres;Password=YOUR_PASSWORD",
    "PHTenantDatabase": "Host=localhost;Port=5433;Database=skybest_trackwise;Username=postgres;Password=YOUR_PASSWORD"
  },
  "TenantDatabaseDefaults": {
    "NameTemplate": "{tenant}_trackwise",
    "Host": "localhost",
    "UserId": "postgres",
    "Password": "YOUR_PASSWORD"
  }
}
```

Per-request the API reads the tenant's own connection string out of the master database, so it needs
no entry for `eu` or `solo`. `PHTenantDatabase` is only the fallback the `TenantDbContext` is
registered with when no tenant has been resolved yet, and `TenantDatabaseDefaults` is what the API
uses to provision a database for a newly created tenant.

Also update `src/Presentation/Trackwise.IdentityServer/appsettings.json` with the same connection string.

The migrator has its own `Tenants[]` array (`src/Presentation/Trackwise.DbMigrator/appsettings.json`)
where each demo tenant carries an explicit connection string. Change the password there too, or the
seed run fails. See [Demo tenants (`Tenants[]`)](../configuration/environment-variables.md#demo-tenants-tenants)
for the full field list.

## Step 4: Seed Databases

Run the database migrator to create tables and seed initial data:

```bash
# Using the provided script
scripts/seed-databases.cmd

# Or manually
dotnet run --project src/Presentation/Trackwise.DbMigrator
```

The migrator applies **all pending migrations automatically** - master DB first, then every tenant DB - so a
newly added migration needs no special step; just run it (or the Docker `migrator` service does it for you).

## Step 5: Run Applications

Open separate terminals for each service:

**Terminal 1 - API**:

```bash
dotnet run --project src/Presentation/Trackwise.API
# Runs on https://localhost:7000
```

**Terminal 2 - Identity Server**:

```bash
dotnet run --project src/Presentation/Trackwise.IdentityServer
# Runs on https://localhost:7001
```

**Terminal 3 - Admin Portal**:

```bash
cd src/Client/Trackwise.Angular
bun run start:admin
# Runs on http://localhost:7002
```

**Terminal 4 - TMS Portal**:

```bash
cd src/Client/Trackwise.Angular
bun run start:tms
# Runs on http://localhost:7003
```

**Terminal 5 - Customer Portal (Optional)**:

```bash
cd src/Client/Trackwise.Angular
bun run start:customer
# Runs on http://localhost:7004
```

Or use the provided scripts:

```bash
scripts/run-api.cmd
scripts/run-identity.cmd
scripts/run-admin.cmd
scripts/run-tms.cmd
scripts/run-customer.cmd
```

## Step 6: Configure Stripe (Optional)

For payment testing:

1. Get your test keys from [Stripe Dashboard](https://dashboard.stripe.com/test/apikeys)

2. Update `src/Presentation/Trackwise.API/appsettings.json`:

   ```json
   {
     "Stripe": {
       "SecretKey": "sk_test_...",
       "WebhookSecret": ""
     }
   }
   ```

3. Run Stripe CLI for webhook forwarding:

   ```bash
   scripts/listen-stripe-webhook.cmd
   ```

4. Copy the webhook secret from Stripe CLI output and update `WebhookSecret`

## Step 7: Access Applications

| Application     | URL                              | Credentials                             |
| --------------- | -------------------------------- | --------------------------------------- |
| API (Swagger)   | <https://localhost:7000/swagger> | -                                       |
| Identity Server | <https://localhost:7001>         | -                                       |
| Admin Portal    | <http://localhost:7002>          | <admin@skybest.com.ph> / Test12345#     |
| TMS Portal      | <http://localhost:7003>          | <owner@skybest.com.ph> / Test12345#     |
| Customer Portal | <http://localhost:7004>          | <customer1@skybest.com.ph> / Test12345# |

Every seeded password is `Test12345#`. The TMS portal logs you into the `ph` tenant; `eu_owner@bestchill.com.ph`
and `solo@skybest.com.ph` get you into the EU fleet and the owner-operator tenant. See
[Test Credentials](test-credentials.md) for the full list.

## Common Issues

### HTTPS Certificate Errors

Trust the development certificate:

```bash
dotnet dev-certs https --trust
```

### Port Already in Use

Check what's using the port:

```bash
# Windows
netstat -ano | findstr :7000

# macOS/Linux
lsof -i :7000
```

### Database Connection Failed

1. Verify PostgreSQL is running
2. Check connection string credentials
3. Ensure database exists

### Angular Build Errors

```bash
cd src/Client/Trackwise.Angular
bun install --force
```

## Next Steps

- [Docker Development](docker-development.md) - Simpler setup with Docker
- [Test Credentials](test-credentials.md) - Full credentials reference
