#!/bin/bash

# ==============================================================================
# Meridian QA Environment - Infrastructure Setup Script (Idempotent)
# ==============================================================================
# Prerequisites:
#   - az login
#   - optional: export ACR_NAME="<existing-registry-name>"
#   - export JWT_SECRET="<your-jwt-secret>"
#   - export DB_PASSWORD="<your-db-password>"
#   - export GOOGLE_MAPS_API_KEY="<your-google-maps-api-key>"
# Safe to re-run — skips any resource that already exists.
# ==============================================================================

set -euo pipefail

# ---------- Validate required secrets ----------
: "${JWT_SECRET:?Please export JWT_SECRET before running this script}"
: "${DB_PASSWORD:?Please export DB_PASSWORD before running this script}"
: "${GOOGLE_MAPS_API_KEY:?Please export GOOGLE_MAPS_API_KEY before running this script}"

# ---------- Configuration ----------
ENV="qa"
LOCATION="eastasia"

RESOURCE_GROUP="rg-meridian-$ENV"

SUFFIX="${ENV:0:3}001"
SQL_SERVER="sql-meridian-$SUFFIX"
CAE_NAME="cae-meridian-$ENV"

DB_ADMIN="meridianadmin"
ACR_NAME="${ACR_NAME:-acrmeridian$ENV}"

echo "🚀 Deploying Meridian Platform ($ENV Environment)..."

# ---------- 1. Resource Group ----------
if az group show --name "$RESOURCE_GROUP" &>/dev/null; then
    echo "⏭️  Resource Group '$RESOURCE_GROUP' already exists, skipping."
else
    echo "📦 Creating Resource Group: $RESOURCE_GROUP"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
fi

# ---------- 2. SQL Server ----------
if az sql server show --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
    echo "⏭️  SQL Server '$SQL_SERVER' already exists, skipping."
else
    echo "🗄️  Creating SQL Server: $SQL_SERVER..."
    az sql server create \
        --name "$SQL_SERVER" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --admin-user "$DB_ADMIN" \
        --admin-password "$DB_PASSWORD"
fi

# Firewall rule (idempotent — create or update)
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER" \
    --name AllowAzureServices \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0

SQL_HOST="$SQL_SERVER.database.windows.net"
CONN_BASE="Server=$SQL_HOST;User ID=$DB_ADMIN;Password=$DB_PASSWORD;Trust Server Certificate=True"

# ---------- 3. Azure Container Registry ----------
if az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
    echo "⏭️  ACR '$ACR_NAME' already exists, skipping."
else
    echo "📦 Creating Azure Container Registry: $ACR_NAME..."
    az acr create \
        --name "$ACR_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --sku Basic \
        --admin-enabled true
fi

# Always ensure admin is enabled (in case the existing ACR had it off)
az acr update --name "$ACR_NAME" --admin-enabled true

ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" -o tsv)

# ---------- 4. Container Apps Environment ----------
if az containerapp env show --name "$CAE_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
    echo "⏭️  Container Apps Environment '$CAE_NAME' already exists, skipping."
else
    echo "☁️  Creating Container Apps Environment: $CAE_NAME..."
    az containerapp env create \
        --name "$CAE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --logs-destination none
fi

# ---------- 5. Container Apps ----------
echo "🛳️  Deploying Microservices to Container Apps..."

REGISTRY_FLAGS="--registry-server $ACR_LOGIN_SERVER --registry-username $ACR_NAME --registry-password $ACR_PASSWORD"
SHARED_ENV=(
    "ASPNETCORE_ENVIRONMENT=QA"
    "Swagger__Enabled=true"
    "Jwt__Issuer=meridian-gateway"
    "Jwt__Audience=meridian-api"
    "Jwt__SecretKey=$JWT_SECRET"
    "Jwt__Secret=$JWT_SECRET"
)
IMAGE_TAG="${IMAGE_TAG:-v1}"

# Helper: create or update a container app
create_app_if_missing() {
    local APP_NAME="$1"
    shift
    
    if az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
        echo "🔄 Container App '$APP_NAME' already exists. Updating image, env, scale, and ingress..."

        local IMAGE_VAL=""
        local MIN_REPLICAS=""
        local MAX_REPLICAS=""
        local TARGET_PORT=""
        local INGRESS_TYPE=""
        local TRANSPORT=""
        local -a ENV_VARS=()

        while [ $# -gt 0 ]; do
            case "$1" in
                --image)
                    IMAGE_VAL="$2"
                    shift 2
                    ;;
                --min-replicas)
                    MIN_REPLICAS="$2"
                    shift 2
                    ;;
                --max-replicas)
                    MAX_REPLICAS="$2"
                    shift 2
                    ;;
                --target-port)
                    TARGET_PORT="$2"
                    shift 2
                    ;;
                --ingress)
                    INGRESS_TYPE="$2"
                    shift 2
                    ;;
                --transport)
                    TRANSPORT="$2"
                    shift 2
                    ;;
                --env-vars)
                    shift
                    while [ $# -gt 0 ] && [[ "$1" != --* ]]; do
                        ENV_VARS+=("$1")
                        shift
                    done
                    ;;
                *)
                    shift
                    ;;
            esac
        done

        local -a UPDATE_ARGS=(--name "$APP_NAME" --resource-group "$RESOURCE_GROUP")
        if [ -n "$IMAGE_VAL" ]; then
            UPDATE_ARGS+=(--image "$IMAGE_VAL")
        fi
        if [ -n "$MIN_REPLICAS" ]; then
            UPDATE_ARGS+=(--min-replicas "$MIN_REPLICAS")
        fi
        if [ -n "$MAX_REPLICAS" ]; then
            UPDATE_ARGS+=(--max-replicas "$MAX_REPLICAS")
        fi
        if [ ${#ENV_VARS[@]} -gt 0 ]; then
            UPDATE_ARGS+=(--set-env-vars "${ENV_VARS[@]}")
        fi

        az containerapp update "${UPDATE_ARGS[@]}" > /dev/null

        local -a INGRESS_ARGS=(--name "$APP_NAME" --resource-group "$RESOURCE_GROUP")
        if [ -n "$TARGET_PORT" ]; then
            INGRESS_ARGS+=(--target-port "$TARGET_PORT")
        fi
        if [ -n "$INGRESS_TYPE" ]; then
            INGRESS_ARGS+=(--type "$INGRESS_TYPE")
        fi
        if [ -n "$TRANSPORT" ]; then
            INGRESS_ARGS+=(--transport "$TRANSPORT")
        fi

        if [ ${#INGRESS_ARGS[@]} -gt 4 ]; then
            az containerapp ingress update "${INGRESS_ARGS[@]}" > /dev/null
        fi
    else
        echo "🚢 Creating Container App: $APP_NAME"
        az containerapp create --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" "$@"
    fi
}

# Get the Environment Default Domain for internal FQDN routing
ACA_DOMAIN=$(az containerapp env show --name "$CAE_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.defaultDomain" -o tsv)
USER_SERVICE_HOST="ca-user-service.internal.$ACA_DOMAIN"
DELIVERY_SERVICE_HOST="ca-delivery-service.internal.$ACA_DOMAIN"
VEHICLE_SERVICE_HOST="ca-vehicle-service.internal.$ACA_DOMAIN"
DRIVER_SERVICE_HOST="ca-driver-service.internal.$ACA_DOMAIN"
ASSIGNMENT_SERVICE_HOST="ca-assignment-service.internal.$ACA_DOMAIN"
ROUTE_SERVICE_HOST="ca-route-service.internal.$ACA_DOMAIN"
TRACKING_SERVICE_HOST="ca-tracking-service.internal.$ACA_DOMAIN"
GATEWAY_ENV=(
    "OCELOT_BASE_URL=https://placeholder"
    "USER_SERVICE_HOST=$USER_SERVICE_HOST"
    "DELIVERY_SERVICE_HOST=$DELIVERY_SERVICE_HOST"
    "VEHICLE_SERVICE_HOST=$VEHICLE_SERVICE_HOST"
    "DRIVER_SERVICE_HOST=$DRIVER_SERVICE_HOST"
    "ASSIGNMENT_SERVICE_HOST=$ASSIGNMENT_SERVICE_HOST"
    "ROUTE_SERVICE_HOST=$ROUTE_SERVICE_HOST"
    "TRACKING_SERVICE_HOST=$TRACKING_SERVICE_HOST"
)

# --- API Gateway ---
create_app_if_missing ca-api-gateway \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-apigateway:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress external \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars \
        "${GATEWAY_ENV[@]}" \
        "${SHARED_ENV[@]}"
GATEWAY_FQDN=$(az containerapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name ca-api-gateway \
    --query "properties.configuration.ingress.fqdn" -o tsv)

az containerapp update \
    --name ca-api-gateway \
    --resource-group "$RESOURCE_GROUP" \
    --set-env-vars "OCELOT_BASE_URL=https://$GATEWAY_FQDN"

# --- User Service ---
create_app_if_missing ca-user-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-userservice:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress internal \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars "ConnectionStrings__UserDb=$CONN_BASE;Initial Catalog=user_db;" "${SHARED_ENV[@]}"

# --- Delivery Service ---
create_app_if_missing ca-delivery-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-deliveryservice:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress internal \
    --transport auto \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars "ConnectionStrings__DeliveryDb=$CONN_BASE;Initial Catalog=meridian_delivery;" "${SHARED_ENV[@]}"

# --- Vehicle Service ---
create_app_if_missing ca-vehicle-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-vehicleservice:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress internal \
    --transport auto \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars "ConnectionStrings__VehicleDb=$CONN_BASE;Initial Catalog=meridian_vehicle;" "${SHARED_ENV[@]}"

# --- Driver Service ---
create_app_if_missing ca-driver-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-driverservice:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress internal \
    --transport auto \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars "ConnectionStrings__DriverDb=$CONN_BASE;Initial Catalog=driver_db;" "${SHARED_ENV[@]}"

# --- Assignment Service ---
create_app_if_missing ca-assignment-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-assignmentservice:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress internal \
    --transport auto \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars \
        "ConnectionStrings__AssignmentDb=$CONN_BASE;Initial Catalog=meridian_assignment;" \
        "Grpc__VehicleServiceUrl=https://$VEHICLE_SERVICE_HOST" \
        "Grpc__DriverServiceUrl=https://$DRIVER_SERVICE_HOST" \
        "Services__DeliveryServiceUrl=https://$DELIVERY_SERVICE_HOST" \
        "${SHARED_ENV[@]}"

# --- Route Service ---
create_app_if_missing ca-route-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-routeservice:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress internal \
    --transport auto \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars \
        "ConnectionStrings__RouteDb=$CONN_BASE;Initial Catalog=meridian_route;" \
        "Grpc__VehicleServiceUrl=https://$VEHICLE_SERVICE_HOST" \
        "GoogleMaps__ApiKey=$GOOGLE_MAPS_API_KEY" \
        "${SHARED_ENV[@]}"

# --- Tracking Service ---
create_app_if_missing ca-tracking-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-trackingservice:$IMAGE_TAG" \
    $REGISTRY_FLAGS \
    --target-port 8080 \
    --ingress internal \
    --transport auto \
    --min-replicas 0 \
    --max-replicas 3 \
    --env-vars \
        "ConnectionStrings__TrackingDb=$CONN_BASE;Initial Catalog=meridian_tracking;" \
        "${SHARED_ENV[@]}"

echo ""
echo "✅ Meridian QA environment deployed!"
GATEWAY_FQDN=$(az containerapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name ca-api-gateway \
    --query "properties.configuration.ingress.fqdn" -o tsv)
echo "🌐 API Gateway: https://$GATEWAY_FQDN"
