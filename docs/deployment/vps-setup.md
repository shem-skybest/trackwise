# VPS Initial Setup

Prepare your Ubuntu/Debian VPS for TrackWise deployment.

## Prerequisites

- Ubuntu 22.04+ LTS or Debian 12+
- Root or sudo access
- SSH access configured

## Step 1: System Update

```bash
sudo apt update && sudo apt upgrade -y
```

## Step 2: Create Deploy User

```bash
sudo adduser trackwise
sudo usermod -aG sudo trackwise
su - trackwise
```

## Step 3: Install Docker

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker

# Verify
docker --version
docker compose version
```

## Step 4: Install Nginx

```bash
sudo apt install nginx -y
sudo systemctl enable nginx
```

## Step 5: Install Certbot (SSL)

```bash
sudo apt install certbot python3-certbot-nginx -y
```

## Step 6: Configure Firewall

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

## Step 7: Deploy the Stack

Production deploys are normally handled by the `deploy.yml` GitHub Actions workflow
(push to the `prod` branch). For a manual first deploy:

```bash
mkdir -p ~/deploy/trackwise && cd ~/deploy/trackwise

# Copy deploy/docker-compose.yml and deploy/postgres-init/ from the repo here, then configure the environment
cp /path/to/repo/deploy/.env.example .env
nano .env  # Edit with your production values, including PostgresUser/PostgresPassword for the containerized postgres service

echo "$GHCR_PAT" | docker login ghcr.io -u <github-user> --password-stdin
docker compose pull && docker compose up -d
```

## Step 8: Configure Nginx

Copy the nginx configuration from `deploy/nginx/trackwise.conf` in the repo:

```bash
sudo cp trackwise.conf /etc/nginx/sites-available/trackwise.conf
sudo ln -s /etc/nginx/sites-available/trackwise.conf /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## Step 9: Obtain SSL Certificates

Only request certs for subdomains that actually have a live nginx server block (see the "Live?" column in [Docker Compose Deployment](docker-deployment.md#subdomains-and-ports)) - certbot's HTTP challenge fails for a hostname nginx doesn't serve yet.

```bash
sudo certbot --nginx \
  -d api.yourdomain.com -d id.yourdomain.com -d admin.yourdomain.com \
  -d app.yourdomain.com -d www.app.yourdomain.com
```

Add `-d customer.yourdomain.com`, `-d yourdomain.com` (website), or `-d portainer.yourdomain.com` once you've uncommented their nginx blocks and given them DNS.

## Step 10: Portainer (manual, one-time)

Portainer runs as its own compose project, kept out of the main stack so that
`docker compose up --remove-orphans` on the app never removes it:

```bash
docker compose -f deploy/docker-compose.portainer.yml up -d
```

Port 9000 is bound to loopback only; it is reachable solely through the
`portainer.skybest.com.ph` server block in `deploy/nginx/trackwise.conf` - currently
commented out until Portainer is actually deployed on this VPS (uncomment it, copy the
config, and run certbot for that hostname once you do).

## Security Recommendations

1. **Disable password SSH**:

   ```bash
   sudo sed -i 's/#PasswordAuthentication yes/PasswordAuthentication no/' /etc/ssh/sshd_config
   sudo systemctl restart sshd
   ```

2. **Install Fail2ban**:

   ```bash
   sudo apt install fail2ban -y
   sudo systemctl enable fail2ban
   ```

3. **Enable automatic updates**:

   ```bash
   sudo apt install unattended-upgrades -y
   sudo dpkg-reconfigure -plow unattended-upgrades
   ```

## Next Steps

- [Docker Compose Deployment](docker-deployment.md) - Service configuration details
- [Environment Variables](../configuration/environment-variables.md) - Configuration reference
