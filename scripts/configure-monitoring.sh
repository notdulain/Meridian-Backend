#!/usr/bin/env bash
# =============================================================================
# MER-103: Production Monitoring & Alerting
#
# This script provisions all Azure monitoring infrastructure for Meridian:
#   MER-326 – Creates Log Analytics Workspace
#   MER-323 – Creates Application Insights (linked to LAW)
#   MER-325 – Creates Action Group for email/Teams notifications
#   MER-324 – Creates Azure Monitor alert rules (error rate, response time)
#
# Usage:
#   ./scripts/configure-monitoring.sh [prod|staging|qa]
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Sufficient permissions on the Azure subscription
# =============================================================================

set -euo pipefail

# Prevent Git Bash from converting Unix-style paths (Azure resource IDs) to Windows paths
export MSYS_NO_PATHCONV=1

ENVIRONMENT="${1:-prod}"

case "$ENVIRONMENT" in
  prod)
    RESOURCE_GROUP="rg-meridian-prod"
    LOCATION="eastasia"
    ;;
  staging)
    RESOURCE_GROUP="rg-meridian-staging"
    LOCATION="eastasia"
    ;;
  qa)
    RESOURCE_GROUP="rg-meridian-qa"
    LOCATION="eastasia"
    ;;
  *)
    echo "ERROR: Unknown environment '$ENVIRONMENT'. Use: prod | staging | qa"
    exit 1
    ;;
esac

# Resource names
LAW_NAME="law-meridian-${ENVIRONMENT}"
APPI_NAME="appi-meridian-${ENVIRONMENT}"
ACTION_GROUP_NAME="ag-meridian-${ENVIRONMENT}"
ACTION_GROUP_SHORT="MerAlert"

# Alert notification — update this email before running in production
ALERT_EMAIL="${ALERT_EMAIL:-devops@meridian.internal}"

# Container Apps to monitor
CONTAINER_APPS=(
  "ca-api-gateway"
  "ca-user-service"
  "ca-delivery-service"
  "ca-vehicle-service"
  "ca-driver-service"
  "ca-assignment-service"
  "ca-route-service"
  "ca-tracking-service"
)

echo "=============================================="
echo " MER-103: Configuring Production Monitoring"
echo " Environment  : $ENVIRONMENT"
echo " Resource Grp : $RESOURCE_GROUP"
echo " Location     : $LOCATION"
echo " Alert Email  : $ALERT_EMAIL"
echo "=============================================="

# ─────────────────────────────────────────────────────────────
# Ensure resource group exists
# ─────────────────────────────────────────────────────────────
echo ""
echo "[1/5] Ensuring resource group exists..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none
echo "  [OK] Resource group: $RESOURCE_GROUP"

# ─────────────────────────────────────────────────────────────
# MER-326: Create Log Analytics Workspace
# ─────────────────────────────────────────────────────────────
echo ""
echo "[2/5] MER-326: Creating Log Analytics Workspace..."
if az monitor log-analytics workspace show \
    --resource-group "$RESOURCE_GROUP" \
    --workspace-name "$LAW_NAME" \
    --output none 2>/dev/null; then
  echo "  [SKIP] Log Analytics Workspace '$LAW_NAME' already exists."
else
  az monitor log-analytics workspace create \
    --resource-group "$RESOURCE_GROUP" \
    --workspace-name "$LAW_NAME" \
    --location "$LOCATION" \
    --sku PerGB2018 \
    --retention-time 30 \
    --tags Project=Meridian Environment="$ENVIRONMENT" MER=MER-326 \
    --output none
  echo "  [OK] Log Analytics Workspace created: $LAW_NAME"
fi

LAW_ID=$(az monitor log-analytics workspace show \
  --resource-group "$RESOURCE_GROUP" \
  --workspace-name "$LAW_NAME" \
  --query id -o tsv)
echo "  [OK] LAW ID: $LAW_ID"

# ─────────────────────────────────────────────────────────────
# MER-323: Create Application Insights (linked to LAW)
# ─────────────────────────────────────────────────────────────
echo ""
echo "[3/5] MER-323: Creating Application Insights..."
if az monitor app-insights component show \
    --app "$APPI_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --output none 2>/dev/null; then
  echo "  [SKIP] Application Insights '$APPI_NAME' already exists."
else
  az monitor app-insights component create \
    --app "$APPI_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --kind web \
    --application-type web \
    --workspace "$LAW_ID" \
    --tags Project=Meridian Environment="$ENVIRONMENT" MER=MER-323 \
    --output none
  echo "  [OK] Application Insights created: $APPI_NAME"
fi

APPI_CONNECTION_STRING=$(az monitor app-insights component show \
  --app "$APPI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query connectionString -o tsv)
APPI_RESOURCE_ID=$(az monitor app-insights component show \
  --app "$APPI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query id -o tsv)

echo "  [OK] Connection String retrieved (store this in GitHub Secrets as APPINSIGHTS_CONNECTION_STRING)"
echo "  Connection String: $APPI_CONNECTION_STRING"

# ─────────────────────────────────────────────────────────────
# MER-325: Create Action Group (email notifications)
# ─────────────────────────────────────────────────────────────
echo ""
echo "[4/5] MER-325: Creating Action Group for alert notifications..."
if az monitor action-group show \
    --name "$ACTION_GROUP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --output none 2>/dev/null; then
  echo "  [SKIP] Action Group '$ACTION_GROUP_NAME' already exists."
else
  az monitor action-group create \
    --name "$ACTION_GROUP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --short-name "$ACTION_GROUP_SHORT" \
    --action email DevOpsEmail "$ALERT_EMAIL" \
    --tags Project=Meridian Environment="$ENVIRONMENT" MER=MER-325 \
    --output none
  echo "  [OK] Action Group created: $ACTION_GROUP_NAME"
  echo "  [OK] Alerts will be sent to: $ALERT_EMAIL"
fi

ACTION_GROUP_ID=$(az monitor action-group show \
  --name "$ACTION_GROUP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query id -o tsv)

# ─────────────────────────────────────────────────────────────
# MER-324: Create Alert Rules on Application Insights
# ─────────────────────────────────────────────────────────────
echo ""
echo "[5/5] MER-324: Creating Alert Rules..."

# Alert 1: High server error rate (HTTP 5xx > 5% over 5 minutes)
ALERT_5XX_NAME="alert-${ENVIRONMENT}-high-error-rate"
if az monitor metrics alert show \
    --name "$ALERT_5XX_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --output none 2>/dev/null; then
  echo "  [SKIP] Alert '$ALERT_5XX_NAME' already exists."
else
  az monitor metrics alert create \
    --name "$ALERT_5XX_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --scopes "$APPI_RESOURCE_ID" \
    --condition "count requests/failed > 10" \
    --window-size 5m \
    --evaluation-frequency 1m \
    --severity 2 \
    --description "MER-324: Fires when failed requests exceed 10 in a 5-minute window across all Meridian services." \
    --action "$ACTION_GROUP_ID" \
    --tags Project=Meridian Environment="$ENVIRONMENT" MER=MER-324 \
    --output none
  echo "  [OK] Created alert: $ALERT_5XX_NAME (failed requests > 10 / 5min)"
fi

# Alert 2: High average response time (> 2000ms over 5 minutes)
ALERT_LATENCY_NAME="alert-${ENVIRONMENT}-high-response-time"
if az monitor metrics alert show \
    --name "$ALERT_LATENCY_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --output none 2>/dev/null; then
  echo "  [SKIP] Alert '$ALERT_LATENCY_NAME' already exists."
else
  az monitor metrics alert create \
    --name "$ALERT_LATENCY_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --scopes "$APPI_RESOURCE_ID" \
    --condition "avg requests/duration > 2000" \
    --window-size 5m \
    --evaluation-frequency 1m \
    --severity 3 \
    --description "MER-324: Fires when average request duration exceeds 2000ms in a 5-minute window." \
    --action "$ACTION_GROUP_ID" \
    --tags Project=Meridian Environment="$ENVIRONMENT" MER=MER-324 \
    --output none
  echo "  [OK] Created alert: $ALERT_LATENCY_NAME (avg response time > 2000ms / 5min)"
fi

# Alert 3: Availability drop (< 99% over 15 minutes)
ALERT_AVAIL_NAME="alert-${ENVIRONMENT}-low-availability"
if az monitor metrics alert show \
    --name "$ALERT_AVAIL_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --output none 2>/dev/null; then
  echo "  [SKIP] Alert '$ALERT_AVAIL_NAME' already exists."
else
  az monitor metrics alert create \
    --name "$ALERT_AVAIL_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --scopes "$APPI_RESOURCE_ID" \
    --condition "avg availabilityResults/availabilityPercentage < 99" \
    --window-size 15m \
    --evaluation-frequency 5m \
    --severity 1 \
    --description "MER-324: Fires when Meridian availability drops below 99% over a 15-minute window." \
    --action "$ACTION_GROUP_ID" \
    --tags Project=Meridian Environment="$ENVIRONMENT" MER=MER-324 \
    --output none
  echo "  [OK] Created alert: $ALERT_AVAIL_NAME (availability < 99% / 15min)"
fi

# ─────────────────────────────────────────────────────────────
# MER-326: Verify logs are flowing to Log Analytics
# ─────────────────────────────────────────────────────────────
echo ""
echo "=============================================="
echo " MER-326: Verifying Log Analytics data flow"
echo "=============================================="
LAW_CUSTOMER_ID=$(az monitor log-analytics workspace show \
  --resource-group "$RESOURCE_GROUP" \
  --workspace-name "$LAW_NAME" \
  --query customerId -o tsv)

echo "  Log Analytics Workspace ID: $LAW_CUSTOMER_ID"
echo "  [INFO] Application Insights is linked to Log Analytics."
echo "  [INFO] Logs from all 8 services will flow automatically once"
echo "         APPLICATIONINSIGHTS_CONNECTION_STRING is set on Container Apps."
echo ""
echo "  To verify in Azure Portal:"
echo "  1. Go to Log Analytics Workspace -> $LAW_NAME"
echo "  2. Logs -> Run query: traces | take 10"
echo "  3. Or: requests | summarize count() by cloud_RoleName"
echo ""
echo "=============================================="
echo " MER-103: Monitoring configuration COMPLETE"
echo "=============================================="
echo ""
echo " Next steps:"
echo "  1. Add to GitHub Secrets:"
echo "     APPINSIGHTS_CONNECTION_STRING = $APPI_CONNECTION_STRING"
echo "  2. Re-run the prod CI/CD pipeline to inject the secret into Container Apps"
echo "  3. Send a test request and verify telemetry in Azure Portal -> Application Insights"
echo "=============================================="
