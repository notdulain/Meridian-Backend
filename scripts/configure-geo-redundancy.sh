#!/usr/bin/env bash
# =============================================================================
# MER-320: Configure Backup Retention Period and Geo-Redundancy
#
# This script configures:
#   1. Long-Term Retention (LTR) - keeps weekly, monthly, yearly backups
#   2. Geo-Redundant Backup Storage - stores backup copies in a paired region
#      so data survives a full Azure region outage
#
# Retention Policy (industry standard):
#   - Short-term : 7 days  (automated daily backups via configure-backups.sh)
#   - Weekly     : 4 weeks (keep the last 4 weekly backups)
#   - Monthly    : 3 months (keep the last 3 monthly backups)
#   - Yearly     : 1 year  (keep the last annual backup)
#
# Usage:
#   ./scripts/configure-geo-redundancy.sh [prod|staging|qa]
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Azure SQL Server must already exist in the target environment
# =============================================================================

set -euo pipefail

ENVIRONMENT="${1:-prod}"

case "$ENVIRONMENT" in
  prod)
    RESOURCE_GROUP="rg-meridian-prod"
    SQL_SERVER="sql-meridian-prod001"
    LOCATION="eastasia"
    # Paired region for East Asia is Southeast Asia
    PAIRED_REGION="southeastasia"
    ;;
  staging)
    RESOURCE_GROUP="rg-meridian-staging"
    SQL_SERVER="sql-meridian-staging001"
    LOCATION="eastasia"
    PAIRED_REGION="southeastasia"
    ;;
  qa)
    RESOURCE_GROUP="rg-meridian-qa"
    SQL_SERVER="sql-meridian-qa001"
    LOCATION="eastasia"
    PAIRED_REGION="southeastasia"
    ;;
  *)
    echo "ERROR: Unknown environment '$ENVIRONMENT'. Use: prod | staging | qa"
    exit 1
    ;;
esac

# All Meridian service databases
DATABASES=(
  "meridian_user"
  "meridian_delivery"
  "meridian_vehicle"
  "meridian_driver"
  "meridian_assignment"
  "meridian_route"
  "meridian_tracking"
)

echo "=========================================="
echo " MER-320: Geo-Redundancy & LTR Setup"
echo " Environment   : $ENVIRONMENT"
echo " SQL Server    : $SQL_SERVER"
echo " Primary Region: $LOCATION"
echo " Backup Region : $PAIRED_REGION"
echo "=========================================="

for db in "${DATABASES[@]}"; do
  echo ""
  echo "Configuring geo-redundancy and LTR for: $db..."

  if az sql db show \
      --resource-group "$RESOURCE_GROUP" \
      --server "$SQL_SERVER" \
      --name "$db" \
      --output none 2>/dev/null; then

    # -------------------------------------------------------------------
    # Step 1: Enable Geo-Redundant Backup Storage
    # This stores backup copies in the paired Azure region automatically.
    # -------------------------------------------------------------------
    echo "  -> Enabling geo-redundant backup storage..."
    az sql db update \
      --resource-group "$RESOURCE_GROUP" \
      --server "$SQL_SERVER" \
      --name "$db" \
      --backup-storage-redundancy Geo \
      --output none
    echo "  [OK] Geo-redundant storage enabled for $db"

    # -------------------------------------------------------------------
    # Step 2: Configure Long-Term Retention (LTR) policy
    # W=4 weeks, M=3 months, Y=1 year, WeekOfYear=1 (first week of year)
    # -------------------------------------------------------------------
    echo "  -> Setting Long-Term Retention policy..."
    az sql db ltr-policy set \
      --resource-group "$RESOURCE_GROUP" \
      --server "$SQL_SERVER" \
      --database "$db" \
      --weekly-retention "P4W" \
      --monthly-retention "P3M" \
      --yearly-retention "P1Y" \
      --week-of-year 1 \
      --output table
    echo "  [OK] LTR policy set: weekly=4w, monthly=3m, yearly=1y"

  else
    echo "  [SKIP] Database '$db' not found on $SQL_SERVER - skipping."
  fi
done

echo ""
echo "=========================================="
echo " MER-320: Geo-Redundancy COMPLETE"
echo ""
echo " All databases now store backup copies in"
echo " '$PAIRED_REGION' (paired region) and have"
echo " long-term retention policies applied."
echo "=========================================="
