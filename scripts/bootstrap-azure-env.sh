#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
    cat <<'EOF'
Usage:
  ./scripts/bootstrap-azure-env.sh <qa|staging|prod>

Required environment variables:
  DB_PASSWORD   SQL admin password for the logical server

Optional environment variables:
  LOCATION      Azure region (default: eastasia)
  DB_ADMIN      SQL admin username (default: meridianadmin)
  ACR_NAME      Override the default ACR name for the environment
EOF
}

ensure_command() {
    local command_name="$1"
    if ! command -v "$command_name" >/dev/null 2>&1; then
        echo "❌ Required command not found: $command_name" >&2
        exit 1
    fi
}

resolve_environment_name() {
    local environment_name="${1:-}"
    case "$environment_name" in
        qa|staging|prod)
            printf '%s\n' "$environment_name"
            ;;
        *)
            usage >&2
            exit 1
            ;;
    esac
}

ensure_command az

register_provider() {
    local namespace="$1"
    local attempts="${2:-60}"
    local sleep_seconds="${3:-10}"
    local state=""

    state="$(az provider show --namespace "$namespace" --query registrationState -o tsv 2>/dev/null || true)"
    if [ "$state" = "Registered" ]; then
        echo "⏭️  Provider '$namespace' is already registered."
        return 0
    fi

    echo "🔧 Registering Azure provider: $namespace"
    az provider register --namespace "$namespace" --output none

    for ((i = 1; i <= attempts; i++)); do
        state="$(az provider show --namespace "$namespace" --query registrationState -o tsv 2>/dev/null || true)"
        if [ "$state" = "Registered" ]; then
            echo "✅ Provider '$namespace' registered."
            return 0
        fi

        echo "⏳ Waiting for provider '$namespace' to register... current state: ${state:-unknown} (${i}/${attempts})"
        sleep "$sleep_seconds"
    done

    echo "❌ Timed out waiting for provider '$namespace' to register." >&2
    echo "   Check manually with: az provider show --namespace $namespace --query registrationState -o tsv" >&2
    exit 1
}

ENVIRONMENT="$(resolve_environment_name "${1:-${ENVIRONMENT:-}}")"
ENVIRONMENT_TAG="$(printf '%s' "$ENVIRONMENT" | tr '[:lower:]' '[:upper:]')"
LOCATION="${LOCATION:-eastasia}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-meridian-$ENVIRONMENT}"
SQL_SERVER="${SQL_SERVER:-sql-meridian-${ENVIRONMENT}001}"
CAE_NAME="${CAE_NAME:-cae-meridian-$ENVIRONMENT}"
ACR_NAME="${ACR_NAME:-acrmeridian$ENVIRONMENT}"
DB_ADMIN="${DB_ADMIN:-meridianadmin}"

: "${DB_PASSWORD:?Please export DB_PASSWORD before running this script}"

echo "🚀 Bootstrapping Meridian Azure resources for '$ENVIRONMENT'..."

az account show >/dev/null
echo "🔌 Ensuring Azure Container Apps CLI extension is installed..."
az extension add --name containerapp --upgrade >/dev/null

for provider in Microsoft.ContainerRegistry Microsoft.Sql Microsoft.App; do
    register_provider "$provider"
done

if az group show --name "$RESOURCE_GROUP" >/dev/null 2>&1; then
    echo "⏭️  Resource Group '$RESOURCE_GROUP' already exists."
else
    echo "📦 Creating Resource Group: $RESOURCE_GROUP"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION" >/dev/null
fi

if az sql server show --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    echo "⏭️  SQL Server '$SQL_SERVER' already exists."
else
    echo "🗄️  Creating SQL Server: $SQL_SERVER"
    az sql server create \
        --name "$SQL_SERVER" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --admin-user "$DB_ADMIN" \
        --admin-password "$DB_PASSWORD" \
        >/dev/null
fi

az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER" \
    --name AllowAzureServices \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    >/dev/null

if az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    echo "⏭️  ACR '$ACR_NAME' already exists."
else
    echo "📦 Creating Azure Container Registry: $ACR_NAME"
    az acr create \
        --name "$ACR_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --sku Basic \
        --admin-enabled true \
        --tags "Project=Meridian" "Environment=$ENVIRONMENT_TAG" \
        >/dev/null
fi

az acr update --name "$ACR_NAME" --admin-enabled true >/dev/null

if az containerapp env show --name "$CAE_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    echo "⏭️  Container Apps Environment '$CAE_NAME' already exists."
else
    echo "☁️  Creating Container Apps Environment: $CAE_NAME"
    az containerapp env create \
        --name "$CAE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --logs-destination none \
        >/dev/null
fi

ACR_LOGIN_SERVER="$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)"

cat <<EOF

✅ Azure bootstrap complete for '$ENVIRONMENT'

Resources:
  Resource Group : $RESOURCE_GROUP
  SQL Server     : $SQL_SERVER
  DB Admin       : $DB_ADMIN
  ACR            : $ACR_NAME
  ACR Login      : $ACR_LOGIN_SERVER
  ACA Env        : $CAE_NAME

Create these Azure SQL databases manually before deploying apps:
  meridian_user
  meridian_delivery
  meridian_vehicle
  meridian_driver
  meridian_assignment
  meridian_route
  meridian_tracking

Next step:
  $SCRIPT_DIR/deploy-${ENVIRONMENT}.sh
EOF
