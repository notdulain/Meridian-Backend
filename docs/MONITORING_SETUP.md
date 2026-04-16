# Monitoring & Alerting Setup — Meridian Platform

**Jira:** MER-95 / MER-103  
**Branch:** `MER-103-production-monitoring-alerting`  
**Implemented:** All 4 subtasks (MER-323, MER-324, MER-325, MER-326)

---

## Overview

This document describes the full observability stack configured for the Meridian microservices platform. Azure Application Insights is wired into all 8 services, alert rules fire on error-rate spikes and latency, notifications go to the DevOps team email, and all logs are centralized in a Log Analytics Workspace.

---

## Architecture

```
All 8 Microservices
    │
    │  Application Insights SDK (MER-323)
    │  APPLICATIONINSIGHTS_CONNECTION_STRING env var
    ▼
Azure Application Insights (appi-meridian-<env>)
    │
    ├── Forwards telemetry to ──►  Log Analytics Workspace (law-meridian-<env>)  [MER-326]
    │
    └── Watched by ──────────────►  Azure Monitor Alert Rules  [MER-324]
                                          │
                                          │  on trigger
                                          ▼
                                   Action Group (ag-meridian-<env>)  [MER-325]
                                          │
                                          ▼
                                   📧 Email Notification → DevOps Team
```

---

## Azure Resources

| Resource | Name Pattern | Purpose |
|----------|-------------|---------|
| Application Insights | `appi-meridian-<env>` | Telemetry collector for all services |
| Log Analytics Workspace | `law-meridian-<env>` | Central log store (30-day retention) |
| Action Group | `ag-meridian-<env>` | Email notification on alert trigger |
| Alert Rule — Error Rate | `alert-<env>-high-error-rate` | Fires if failed requests > 10 in 5 min |
| Alert Rule — Latency | `alert-<env>-high-response-time` | Fires if avg response time > 2000ms |
| Alert Rule — Availability | `alert-<env>-low-availability` | Fires if availability drops below 99% |

---

## MER-323: Application Insights in Each Service

### What was changed

**All 8 microservices** received the following changes:

1. **`*.csproj`** — Added NuGet package:
   ```xml
   <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />
   ```

2. **`Program.cs`** — Added one line after `AddControllers()`:
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

### Services updated
- `ApiGateway`
- `UserService`
- `DeliveryService`
- `VehicleService`
- `DriverService`
- `AssignmentService`
- `RouteService`
- `TrackingService`

### How the connection string is injected

The SDK reads the connection string from the environment variable `APPLICATIONINSIGHTS_CONNECTION_STRING` automatically — no code change needed. This variable is injected into every Container App via `SHARED_ENV` in the CI/CD pipeline.

---

## MER-324: Alert Rules

Three alert rules are created by `scripts/configure-monitoring.sh`:

| Alert | Condition | Window | Severity |
|-------|-----------|--------|----------|
| High Error Rate | Failed requests > 10 | 5 min | 2 (Warning) |
| High Response Time | Avg response > 2000ms | 5 min | 3 (Informational) |
| Low Availability | Availability < 99% | 15 min | 1 (Critical) |

### Viewing alerts in Azure Portal
1. Azure Portal → **Monitor** → **Alerts**
2. Filter by Resource Group: `rg-meridian-<env>`
3. Click any alert to see history and status

---

## MER-325: Alert Notification Channel

An **Action Group** is created that sends email alerts to the configured `ALERT_EMAIL`.

### Configuring the email address

Before running `configure-monitoring.sh` in production, set the recipient email:

```bash
export ALERT_EMAIL="your-team@example.com"
./scripts/configure-monitoring.sh prod
```

Or update the default inside the script:
```bash
ALERT_EMAIL="${ALERT_EMAIL:-devops@meridian.internal}"
```

### Adding Teams webhook (optional)

To also notify via Microsoft Teams, update the Action Group in Azure Portal:
1. **Monitor** → **Action Groups** → `ag-meridian-prod`
2. **Add action** → **Webhook** → Paste your Teams incoming webhook URL

---

## MER-326: Log Analytics Workspace

All telemetry from Application Insights is stored in the Log Analytics Workspace `law-meridian-<env>`.

### Verifying logs are flowing

1. Azure Portal → **Log Analytics workspaces** → `law-meridian-prod`
2. Click **Logs** and run:

```kusto
// All telemetry from all services
traces
| take 20

// Request counts per service
requests
| summarize count() by cloud_RoleName
| order by count_ desc

// Failed requests in the last hour
requests
| where success == false
| where timestamp > ago(1h)
| summarize count() by cloud_RoleName, resultCode

// Slow requests (> 2 seconds)
requests
| where duration > 2000
| project timestamp, cloud_RoleName, name, duration, resultCode
| order by duration desc
```

---

## Setup Guide (First-Time)

### Step 1: Run the monitoring script

```bash
# Set your alert email
export ALERT_EMAIL="your-email@company.com"

# Run for QA first to test
./scripts/configure-monitoring.sh qa

# Then production
./scripts/configure-monitoring.sh prod
```

### Step 2: Note the output Connection String

The script will print:
```
Connection String: InstrumentationKey=...;IngestionEndpoint=...
```

### Step 3: Update GitHub Secrets (optional)

The CI/CD pipeline automatically retrieves the connection string from Azure during deployment, so no manual secret is needed. However, if you want to set it manually:

- **Secret name:** `APPINSIGHTS_CONNECTION_STRING`
- **Value:** The connection string from Step 2

### Step 4: Trigger a deployment

Push a new tag to trigger the prod CI/CD pipeline. It will automatically provision Application Insights and inject the connection string into all Container Apps.

### Step 5: Verify telemetry

1. Make a request to the API Gateway
2. Wait ~2 minutes
3. Open Azure Portal → **Application Insights** → `appi-meridian-prod` → **Live Metrics**

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| No data in App Insights | Verify `APPLICATIONINSIGHTS_CONNECTION_STRING` env var is set on the Container App |
| Alert not firing | Check alert rule is in "Enabled" state in Azure Monitor |
| No email received | Verify Action Group email address is correct and check spam |
| Logs not in Log Analytics | Verify Container Apps Environment is linked to LAW (check `configure-monitoring.sh` ran successfully) |
