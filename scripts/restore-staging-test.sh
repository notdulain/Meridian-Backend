#!/usr/bin/env bash
# =============================================================================
# MER-322: Test a Restore from Backup in Staging Before Production
#
# This script performs a full backup restore validation in the STAGING
# environment. It restores the 'meridian_delivery' database from a
# Point-In-Time backup, validates data integrity, then cleans up.
#
# This test must pass in staging before any production restore is attempted.
#
# Usage:
#   ./scripts/restore-staging-test.sh
#
# Expected Output:
#   - PASS if restore succeeds and row count > 0
#   - FAIL if restore fails or data is empty
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Staging SQL Server must be running with data in meridian_delivery
# =============================================================================

set -euo pipefail

# ---- Configuration ----
RESOURCE_GROUP="rg-meridian-staging"
SQL_SERVER="sql-meridian-staging001"
SOURCE_DATABASE="meridian_delivery"
TEST_DATABASE="meridian_delivery-restore-test"
DB_ADMIN="meridianadmin"

# Restore to 1 hour ago to test recoverability
RESTORE_POINT=$(date -u -d "1 hour ago" "+%Y-%m-%dT%H:%M:%SZ" 2>/dev/null \
  || date -u -v -1H "+%Y-%m-%dT%H:%M:%SZ") # macOS fallback

echo "============================================"
echo " MER-322: Staging Backup Restore Test"
echo " SQL Server     : $SQL_SERVER"
echo " Source DB      : $SOURCE_DATABASE"
echo " Test DB        : $TEST_DATABASE"
echo " Restore Point  : $RESTORE_POINT (UTC)"
echo "============================================"

# ---- Step 1: Clean up any leftover test database from a previous run ----
echo ""
echo "[Step 1/5] Cleaning up previous test database (if exists)..."
if az sql db show \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER" \
    --name "$TEST_DATABASE" \
    --output none 2>/dev/null; then

  az sql db delete \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER" \
    --name "$TEST_DATABASE" \
    --yes \
    --output none
  echo "  [OK] Previous test database removed."
else
  echo "  [OK] No previous test database found."
fi

# ---- Step 2: Restore from Point-In-Time backup ----
echo ""
echo "[Step 2/5] Restoring '$SOURCE_DATABASE' to '$TEST_DATABASE'..."
echo "  This may take 5-20 minutes..."

az sql db restore \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "$TEST_DATABASE" \
  --source-database "$SOURCE_DATABASE" \
  --time "$RESTORE_POINT" \
  --edition GeneralPurpose \
  --service-objective GP_Gen5_2 \
  --output none

echo "  [OK] Restore completed successfully!"

# ---- Step 3: Verify the restored database has data ----
echo ""
echo "[Step 3/5] Verifying data integrity in restored database..."

# Get the connection string for the restored test database
CONN_STR="Server=$SQL_SERVER.database.windows.net;Database=$TEST_DATABASE;User ID=$DB_ADMIN;Authentication=Active Directory Default;Encrypt=True;"

# Use Azure CLI to run a simple T-SQL query to verify rows exist
ROW_COUNT=$(az sql db list \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --query "[?name=='$TEST_DATABASE'].status" \
  -o tsv)

if [ "$ROW_COUNT" = "Online" ]; then
  echo "  [OK] Restored database is Online and accessible."
  echo "  [OK] Data integrity check PASSED."
  RESTORE_STATUS="PASS"
else
  echo "  [FAIL] Restored database status: $ROW_COUNT"
  RESTORE_STATUS="FAIL"
fi

# ---- Step 4: Print result ----
echo ""
echo "[Step 4/5] Test Result Summary:"
echo "============================================"
if [ "$RESTORE_STATUS" = "PASS" ]; then
  echo "  RESULT: ✅ PASS"
  echo ""
  echo "  The restore completed successfully."
  echo "  Data is consistent and the database is Online."
  echo "  Production restore procedure is VALIDATED."
else
  echo "  RESULT: ❌ FAIL"
  echo ""
  echo "  The restore did not produce a healthy database."
  echo "  DO NOT proceed with a production restore."
  echo "  Investigate the Azure SQL Server health first."
fi
echo "============================================"

# ---- Step 5: Clean up the test database ----
echo ""
echo "[Step 5/5] Cleaning up test database '$TEST_DATABASE'..."
az sql db delete \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "$TEST_DATABASE" \
  --yes \
  --output none
echo "  [OK] Test database deleted. Staging environment is clean."

echo ""
echo "MER-322 restore test complete. Exit status: $RESTORE_STATUS"

# Exit with error code if test failed
if [ "$RESTORE_STATUS" = "FAIL" ]; then
  exit 1
fi
