# Meridian Disaster Recovery Plan (DRP)

This document outlines the Disaster Recovery Plan for the Meridian microservices platform, covering RPO/RTO targets, Azure resource failover strategies, and a total environment manual recovery runbook.

## 1. RTO and RPO Targets (MER-327)

*   **RPO (Recovery Point Objective):** The maximum acceptable amount of data loss measured in time.
*   **RTO (Recovery Time Objective):** The maximum acceptable duration of downtime before services are restored.

| Service/Component | RPO Target | RTO Target | Justification / Mechanism |
| :--- | :--- | :--- | :--- |
| **Azure SQL Databases** (7 dbs) | **15 Minutes** | **1 Hour** | Critical state. Relying on Azure SQL built-in Point-in-Time Restore (PITR) and Geo-Replication. |
| **Microservices / Container Apps** | **N/A (Stateless)** | **30 Minutes** | Services are stateless. RTO is based on the time to spin up new Azure Container Apps environments. |
| **Redis Cloud** | **0 Minutes** (Transient) | **30 Minutes** | Redis only acts as a caching layer for the RouteService. No critical state is lost if rebuilt. |
| **Azure Container Registry (ACR)** | **N/A** | **30 Minutes** | Images can be rebuilt from GitHub Actions if the registry is permanently lost, or served via ACR geo-replication. |

---

## 2. Failover Steps for Azure Resources (MER-328)

In the event of a single service or partial region failure, these are the immediate failover steps:

### Azure SQL Database
1. Navigate to the Azure Portal -> SQL servers -> `sql-meridian-<env>001`.
2. If geo-replication is enabled, select "Failover" to promote the secondary database to primary.
3. If the primary region is completely dead without geo-replication active, use the **Geo-Restore** feature from the latest backup to a SQL server in a secondary region (e.g., `westus`).
4. Update Azure Key Vault with the new SQL Server connection strings.

### Azure Container Apps (CAE / Microservices)
1. By default, Azure Container Apps handles internal node failures automatically.
2. For an entire regional failure, traffic must be routed to a standby Container Apps Environment (CAE) in a secondary region.
3. Use the deployment script (found in `AZURE_DEPLOYMENT_GUIDE.md`) to re-provision the CAE, referencing the existing Azure Container Registry (ACR).

### Redis Cloud (RouteService Cache)
1. If the Redis Cloud instance goes offline, log in to the Redis Cloud console.
2. Provision a new cache instance if the current one is unrecoverable.
3. Update the Redis Connection String in Azure Key Vault. Reboot the `ca-route-service` Container App.

### Azure Key Vault
1. Standard Azure Key Vault automatically fails over to the paired region for read availability.
2. If the vault is soft-deleted or requires complete restoration, restore it from the latest manual backup.

---

## 3. Manual Recovery Runbook for Total Environment Loss (MER-329)

If the entire primary Azure region (e.g., `eastasia`) suffers an unrecoverable catastrophic failure, execute the following steps to rebuild Meridian in a secondary region (e.g., `southeastasia`).

### Step 1: Declare Disaster & Notify
*   Confirm total regional outage via Azure Service Health.
*   Notify stakeholders, DevOps team, and engineering leads.

### Step 2: Establish New Resource Group
*   Log in to Azure CLI (`az login`).
*   Create a new Resource Group in the paired region:
    `az group create --name rg-meridian-prod-failover --location southeastasia`

### Step 3: Restore Azure SQL Databases
*   Create a new Azure SQL Server in the new Resource Group.
*   Navigate to the portal and perform a **Geo-Restore** for all 7 Meridian databases (`meridian_user`, `meridian_delivery`, `meridian_vehicle`, `meridian_driver`, `meridian_assignment`, `meridian_route`, `meridian_tracking`).

### Step 4: Setup Key Vault & Secrets
*   Create a new Azure Key Vault (or rely on Key Vault's automatic paired-region failover).
*   Populate the Key Vault with the new database connection strings, JWT configs, and Redis connection strings.

### Step 5: Provision Container Architecture
*   If ACR is unavailable, recreate ACR and trigger GitHub Actions to rebuild all 8 microservices Docker images.
*   Once images are in ACR, provision the new Container Apps Environment (CAE).
*   Deploy the 8 microservices (`ca-api-gateway`, etc.) using the restored database and Key Vault configuration.

### Step 6: Reroute Traffic & Test
*   Update the main DNS A-records or Traffic Manager to point to the new `ca-api-gateway` FQDN.
*   Ensure Web Socket (SignalR) connections route correctly to `ca-tracking-service`.
*   Run automated smoke tests to verify the driver, user, and tracking endpoints are fully operational.
