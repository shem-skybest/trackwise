# Docker Compose Deployment

TrackWise deploys as a set of containers defined by a hand-maintained Docker Compose file under [`deploy/`](https://github.com/shem-skybest/skybest-logistics-corporation/tree/main/deploy). The host runs plain `docker compose`; nginx (on the host) terminates TLS and reverse-proxies each subdomain to a loopback-bound container port.

## Layout

```text
deploy/
  docker-compose.yml            # main production stack (incl. containerized postgres)
  docker-compose.dev.yml        # local dev infra (Postgres + migrator)
  docker-compose.portainer.yml  # Portainer (separate stack, one-time manual deploy)
  postgres-init/                # SQL run once against a fresh postgres volume (creates tenant DBs)
  .env.example                  # template for the DOCKER_ENV secret / .env
  Run-ProdMigrator.ps1          # deliberate prod DB migrate+seed (loads .env, typed confirmation)
  nginx/trackwise.conf         # host nginx reverse proxy (subdomains -> 127.0.0.1:port)
```

The main stack contains `postgres`, `identity-server`, `api`, `admin-portal`, `tms-portal`, `customer-portal`, and `website`. PostgreSQL runs as its own service in the compose file, with a named volume for data and `postgres-init/` creating the tenant databases on first boot.

## Automated deployment (recommended)

Deployment is handled by the [`deploy.yml`](https://github.com/shem-skybest/skybest-logistics-corporation/blob/main/.github/workflows/deploy.yml) GitHub Actions workflow. Pushing to the `prod` branch (or running it manually) will:

1. Build and push all seven images to GHCR (the six services plus `dbmigrator`).
2. Copy `deploy/docker-compose.yml` to `~/deploy/trackwise/` on the VPS.
3. Write `.env` from the `DOCKER_ENV` secret and append `GITHUB_REPOSITORY` + `IMAGE_TAG`.
4. `docker compose pull && docker compose up -d --force-recreate --remove-orphans`.

Required GitHub secrets: `SSH_HOST`, `SSH_USER`, `SSH_KEY`, `GHCR_PAT`, `DOCKER_ENV` (the full `.env` contents), `FIREBASE_CREDENTIALS_JSON`.

## Manual deployment

Use this on first setup or when deploying outside CI. See [VPS Setup](vps-setup.md) first.

### 1. Copy the stack to the VPS

```bash
mkdir -p ~/deploy/trackwise && cd ~/deploy/trackwise
# Copy deploy/docker-compose.yml here, then:
cp /path/to/repo/deploy/.env.example .env
nano .env   # fill in production values (see Environment Variables)
```

`GITHUB_REPOSITORY` and `IMAGE_TAG` are appended automatically by CI; for a manual run, either export them or rely on the compose defaults (`shem-skybest/skybest-logistics-corporation` + `latest`).

### 2. Log in to GHCR and start

```bash
echo "$GHCR_PAT" | docker login ghcr.io -u <github-user> --password-stdin
docker compose pull
docker compose up -d
```

### 3. Configure nginx + SSL

See [VPS Setup](vps-setup.md) for the nginx copy + certbot steps.

### 4. Verify

```bash
curl -sf http://127.0.0.1:7000/health
curl http://127.0.0.1:7001/.well-known/openid-configuration
docker compose ps
```

## Subdomains and ports

This table is the canonical port reference - `deploy/docker-compose.yml` and `deploy/nginx/trackwise.conf` must both agree with it.

| Subdomain                  | Host port (loopback)  | Override variable      | Live?                          |
| -------------------------- | --------------------- | ---------------------- | ------------------------------ |
| `api.skybest.com.ph`       | 7000                  | `API_PORT`             | yes                            |
| `id.skybest.com.ph`        | 7001                  | `IDENTITY_SERVER_PORT` | yes                            |
| `admin.skybest.com.ph`     | 7002                  | `ADMIN_PORTAL_PORT`    | yes                            |
| `trackwise.skybest.com.ph` | 7003                  | `TMS_PORTAL_PORT`      | yes                            |
| `customer.skybest.com.ph`  | 7004                  | `CUSTOMER_PORTAL_PORT` | no - nginx block commented out |
| `skybest.com.ph` (website) | 7005                  | `WEBSITE_PORT`         | no - nginx block commented out |
| `portainer.skybest.com.ph` | 9000 (separate stack) | -                      | no - nginx block commented out |

All app ports bind to `127.0.0.1`, so the containers are reachable only through nginx. The override variables are optional; compose falls back to the defaults above when they are absent from `.env`. Changing one means editing the matching `proxy_pass` in the nginx config too. To bring a "not live" service online: give it DNS, uncomment its server block in `deploy/nginx/trackwise.conf`, copy that file to the VPS, reload nginx, and run certbot for the new hostname - none of that is automated by `deploy.yml`.

## Database migrations

Migrations are not run automatically in production. `deploy.yml` builds a `dbmigrator` image alongside the others, and the compose file declares it under the `migrate` profile, so `up` never starts it. Run it on the VPS, from `~/deploy/trackwise`, after a deploy that brings new migrations:

```bash
docker compose --profile migrate pull dbmigrator
docker compose run --rm dbmigrator
docker compose restart identity-server api
```

It reads the same `.env` as the stack, so no credentials leave the server. The `dbmigrator` service supplies the tenant connection strings (`ph_trackwise`, `eu_trackwise`) from `PostgresPassword`.

In Production the migrator seeds only system data (roles, plans, tax rates, super admin) and the seeders that opt in with `SeedsInProduction`: the real staff, crew, fleet, trailers, permits, repair history, reefer PMS, clients, outlets, rate agreements, business partners and branches. Demo data and `DemoOnly` tenants (`solo`) are skipped, and seed users get no password; only the super admin can sign in until others are invited. Every seeder is idempotent, so a rerun adds only what is missing.

`deploy/Run-ProdMigrator.ps1` remains as the fallback for running from a workstation over an SSH tunnel. It loads `deploy/.env`, forces Production (with `--no-launch-profile`, since `launchSettings.json` would otherwise switch it to Development) and requires you to type `migrate-prod`. See [Environment Variables](../configuration/environment-variables.md#running-the-migrator-against-production) for the variables it needs.

The migrator also applies the **Duende operational store** schema to the master DB (`Keys`, `PersistedGrants`, ...), where the IdentityServer persists signing keys and refresh tokens so sessions survive redeploys. Run the migrator **before** deploying an IdentityServer image that expects those tables. The first deploy after introducing this store still logs everyone out once; later redeploys keep sessions alive.

## Service management

```bash
docker compose logs -f            # all services
docker compose logs -f api        # one service
docker compose restart
docker compose pull && docker compose up -d   # update images
```

## Portainer

Portainer runs as its own compose project (`deploy/docker-compose.portainer.yml`) so a main-stack redeploy (`--remove-orphans`) never removes it. It is deployed once, manually - see [VPS Setup](vps-setup.md). Its nginx block (`portainer.skybest.com.ph` → `127.0.0.1:9000`) is currently commented out in `deploy/nginx/trackwise.conf` since Portainer isn't deployed on the current VPS; the port is never exposed publicly regardless.

## Troubleshooting

```bash
docker compose ps
docker compose logs api --tail 100
docker compose logs identity-server
sudo nginx -t && sudo tail -f /var/log/nginx/error.log
```

## Next Steps

- [VPS Setup](vps-setup.md) - initial server configuration
- [Environment Variables](../configuration/environment-variables.md) - full configuration reference
