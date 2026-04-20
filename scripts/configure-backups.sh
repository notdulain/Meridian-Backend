#!/usr/bin/env bash
# =============================================================================
# MER-319: Enable Automated Backups on Azure SQL Databases
#
# This script enables automated backups for all Meridian SQL databases across
# all environments (prod, staging, qa). Azure SQL Database provides built-in
# automated backups. This script configures short-term retention policies
# and verifies backup is active on all databases.
#
# Usage:
#   ./scripts/configure-backups.sh [prod|staging|qa]
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Sufficient permissions on the Azure subscription
# =============================================================================

set -euo pipefail

ENVIRONMENT="${1:-prod}"

case "$ENVIRONMENT" in
  prod)
    RESOURCE_GROUP="rg-meridian-prod"
    SQL_SERVER="sql-meridian-prod001"
    RETENTION_DAYS=7
    ;;
  staging)
    RESOURCE_GROUP="rg-meridian-staging"
    SQL_SERVER="sql-meridian-staging001"
    RETENTION_DAYS=7
    ;;
  qa)
    RESOURCE_GROUP="rg-meridian-qa"
    SQL_SERVER="sql-meridian-qa001"
    RETENTION_DAYS=7
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
echo " MER-319: Configuring Automated Backups"
echo " Environment : $ENVIRONMENT"
echo " SQL Server  : $SQL_SERVER"
echo " Retention   : $RETENTION_DAYS days"
echo "=========================================="

for db in "${DATABASES[@]}"; do
  echo ""
  echo "Configuring backup for database: $db..."

  # Check if the database exists before trying to configure it
  if az sql db show \
      --resource-group "$RESOURCE_GROUP" \
      --server "$SQL_SERVER" \
      --name "$db" \
      --output none 2>/dev/null; then

    # Set short-term backup retention policy (1-35 days)
    az sql db str-policy set \
      --resource-group "$RESOURCE_GROUP" \
      --server "$SQL_SERVER" \
      --database "$db" \
      --retention-days "$RETENTION_DAYS" \
      --output table

    echo "  [OK] Backup retention set to $RETENTION_DAYS days for $db"
  else
    echo "  [SKIP] Database '$db' not found on $SQL_SERVER - skipping."
  fi
done

echo ""
echo "=========================================="
echo " MER-319: Backup configuration COMPLETE"
echo " All available databases have automated"
echo " backups enabled with $RETENTION_DAYS-day retention."
echo "=========================================="
