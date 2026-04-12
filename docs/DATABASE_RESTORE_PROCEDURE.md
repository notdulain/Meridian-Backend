# MER-321: Database Backup & Manual Restore Procedure

## Overview

This document describes how to manually restore any Meridian database from an automated Azure SQL backup in the event of data loss, corruption, or accidental deletion.

> **When to use this guide:** If production data has been lost, corrupted, or a database has been accidentally dropped, follow these steps immediately. Contact the tech lead before performing a restore in production.

---

## Database Inventory

| Database Name        | Service           | SQL Server (Prod)          | SQL Server (Staging)           |
|----------------------|-------------------|----------------------------|--------------------------------|
| `meridian_user`      | User Service      | `sql-meridian-prod001`     | `sql-meridian-staging001`      |
| `meridian_delivery`  | Delivery Service  | `sql-meridian-prod001`     | `sql-meridian-staging001`      |
| `meridian_vehicle`   | Vehicle Service   | `sql-meridian-prod001`     | `sql-meridian-staging001`      |
| `meridian_driver`    | Driver Service    | `sql-meridian-prod001`     | `sql-meridian-staging001`      |
| `meridian_assignment`| Assignment Service| `sql-meridian-prod001`     | `sql-meridian-staging001`      |
| `meridian_route`     | Route Service     | `sql-meridian-prod001`     | `sql-meridian-staging001`      |
| `meridian_tracking`  | Tracking Service  | `sql-meridian-prod001`     | `sql-meridian-staging001`      |

---

## Backup Policy

| Backup Type     | Frequency      | Retention     | Storage        |
|-----------------|----------------|---------------|----------------|
| Full backup     | Weekly         | 7 days        | Geo-Redundant  |
| Differential    | Every 12 hours | 7 days        | Geo-Redundant  |
| Transaction log | Every 5–10 min | 7 days        | Geo-Redundant  |
| Weekly (LTR)    | Weekly         | 4 weeks       | Geo-Redundant  |
| Monthly (LTR)   | Monthly        | 3 months      | Geo-Redundant  |
| Yearly (LTR)    | Yearly         | 1 year        | Geo-Redundant  |

> Azure SQL Database performs automated backups. The scripts `configure-backups.sh` and `configure-geo-redundancy.sh` in `/scripts` configure these policies.

---

## Prerequisites

Before starting a restore, ensure you have:

- [ ] Azure CLI installed: `az --version`
- [ ] Logged in to Azure: `az login`
- [ ] Sufficient RBAC permissions on the subscription (Contributor or SQL DB Contributor)
- [ ] Confirmed the approximate date/time the data was last known to be good

---

## Step-by-Step Restore Procedure

### Step 1 — Identify the target database and restore point

Decide:
- **Which database** needs to be restored (e.g., `meridian_delivery`)
- **Which environment** (prod or staging)
- **What time** to restore to (Point-In-Time Restore allows restoring to any second in the last 7 days)

### Step 2 — List available backup restore points (optional verification)

```bash
# Replace values as needed
RESOURCE_GROUP="rg-meridian-prod"
SQL_SERVER="sql-meridian-prod001"
DATABASE="meridian_delivery"

az sql db list-deleted \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER"
```

### Step 3 — Perform a Point-In-Time Restore (PITR)

Azure SQL restores to a **new database** first (it cannot overwrite the live one while it is running). Name it with a `-restored` suffix.

```bash
RESOURCE_GROUP="rg-meridian-prod"
SQL_SERVER="sql-meridian-prod001"
SOURCE_DATABASE="meridian_delivery"
RESTORED_DATABASE="meridian_delivery-restored"

# Set the exact UTC time to restore to (must be within last 7 days)
RESTORE_POINT="2025-04-10T14:30:00Z"

az sql db restore \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "$RESTORED_DATABASE" \
  --source-database "$SOURCE_DATABASE" \
  --time "$RESTORE_POINT" \
  --edition GeneralPurpose \
  --service-objective GP_Gen5_2 \
  --output table
```

> ⏳ The restore typically takes **5–20 minutes** depending on database size.

### Step 4 — Verify the restored data

Connect to the restored database and confirm the data is correct:

```bash
# Use the Azure Portal Query Editor or SQL Server Management Studio (SSMS)
# Connection string format:
# Server: sql-meridian-prod001.database.windows.net
# Database: meridian_delivery-restored
# Authentication: SQL Login (meridianadmin)
```

Run a quick sanity check:
```sql
-- Confirm row counts look reasonable
SELECT COUNT(*) FROM Deliveries;
SELECT TOP 10 * FROM Deliveries ORDER BY CreatedAt DESC;
```

### Step 5 — Swap the restored database into production

Once verified, rename the databases to swap them:

```bash
RESOURCE_GROUP="rg-meridian-prod"
SQL_SERVER="sql-meridian-prod001"

# Step 5a: Rename the broken live database to a backup name
az sql db rename \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "meridian_delivery" \
  --new-name "meridian_delivery-broken-$(date +%Y%m%d%H%M)"

# Step 5b: Rename the restored database to the production name
az sql db rename \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "meridian_delivery-restored" \
  --new-name "meridian_delivery"
```

### Step 6 — Restart affected microservices

After the database swap, force-restart the affected container app to clear connection pools:

```bash
RESOURCE_GROUP="rg-meridian-prod"

az containerapp revision restart \
  --resource-group "$RESOURCE_GROUP" \
  --name ca-delivery-service
```

### Step 7 — Verify the live service is healthy

```bash
# Check the container app status
az containerapp show \
  --resource-group "rg-meridian-prod" \
  --name ca-delivery-service \
  --query "properties.runningStatus"

# Optionally hit the API Gateway health endpoint
curl -s https://<gateway-fqdn>/diagnostics
```

### Step 8 — Post-Incident Cleanup

Once production is confirmed healthy:

```bash
# Remove the old broken database
az sql db delete \
  --resource-group "rg-meridian-prod" \
  --server "sql-meridian-prod001" \
  --name "meridian_delivery-broken-<timestamp>" \
  --yes
```

---

## Restore from Long-Term Retention (LTR) Backup

If the 7-day window has passed and you need an older backup:

```bash
# List available LTR backups
az sql db ltr-backup list \
  --location eastasia \
  --server sql-meridian-prod001 \
  --database meridian_delivery

# Restore from a specific LTR backup
az sql db ltr-backup restore \
  --backup-id "<backup-resource-id-from-above>" \
  --dest-database meridian_delivery-ltr-restored \
  --dest-server sql-meridian-prod001 \
  --dest-resource-group rg-meridian-prod
```

---

## Emergency Contacts

| Role             | Responsibility                        |
|------------------|---------------------------------------|
| DevOps Engineer  | Executes restore procedure            |
| Tech Lead        | Approves production restore           |
| Team Lead / PM   | Notifies stakeholders of data outage  |

---

## Related Scripts

| Script                          | Purpose                                    |
|---------------------------------|--------------------------------------------|
| `scripts/configure-backups.sh`  | Enable automated daily backups (MER-319)  |
| `scripts/configure-geo-redundancy.sh` | Set LTR and geo-redundancy (MER-320) |
| `scripts/restore-staging-test.sh` | Run restore validation on staging (MER-322) |
