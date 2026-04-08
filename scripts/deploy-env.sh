#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
    cat <<'EOF'
Usage:
  ./scripts/deploy-env.sh <qa|staging|prod>

Required environment variables:
  DB_PASSWORD
  JWT_SECRET
  GOOGLE_MAPS_API_KEY
  REDIS_CONNECTION_STRING

Optional environment variables:
  IMAGE_TAG      Container image tag to deploy (default: v1)
  LOCATION       Azure region (default: eastasia)
  DB_ADMIN       SQL admin username (default: meridianadmin)
  ACR_NAME       Override the default ACR name for the environment
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

resolve_aspnet_environment() {
    local environment_name="$1"
    case "$environment_name" in
        qa) printf 'QA\n' ;;
        staging) printf 'STAGING\n' ;;
        prod) printf 'PROD\n' ;;
    esac
}

ensure_command az

ENVIRONMENT="$(resolve_environment_name "${1:-${ENVIRONMENT:-}}")"
ASPNETCORE_ENVIRONMENT="$(resolve_aspnet_environment "$ENVIRONMENT")"
LOCATION="${LOCATION:-eastasia}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-meridian-$ENVIRONMENT}"
SQL_SERVER="${SQL_SERVER:-sql-meridian-${ENVIRONMENT}001}"
CAE_NAME="${CAE_NAME:-cae-meridian-$ENVIRONMENT}"
ACR_NAME="${ACR_NAME:-acrmeridian$ENVIRONMENT}"
DB_ADMIN="${DB_ADMIN:-meridianadmin}"
IMAGE_TAG="${IMAGE_TAG:-v1}"

: "${DB_PASSWORD:?Please export DB_PASSWORD before running this script}"
: "${JWT_SECRET:?Please export JWT_SECRET before running this script}"
: "${GOOGLE_MAPS_API_KEY:?Please export GOOGLE_MAPS_API_KEY before running this script}"
: "${REDIS_CONNECTION_STRING:?Please export REDIS_CONNECTION_STRING before running this script}"

"$SCRIPT_DIR/bootstrap-azure-env.sh" "$ENVIRONMENT"

echo "🛳️  Deploying Meridian apps to '$ENVIRONMENT'..."

SQL_HOST="$SQL_SERVER.database.windows.net"
CONN_BASE="Server=$SQL_HOST;User ID=$DB_ADMIN;Password=$DB_PASSWORD;Trust Server Certificate=True"
ACR_LOGIN_SERVER="$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)"
ACR_PASSWORD="$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" -o tsv)"

create_or_update_app() {
    local app_name="$1"
    shift

    if az containerapp show --name "$app_name" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
        echo "🔄 Updating $app_name..."
        local image_val=""
        local min_replicas=""
        local max_replicas=""
        local -a env_vars=()

        while [ $# -gt 0 ]; do
            case "$1" in
                --image)
                    image_val="$2"
                    shift 2
                    ;;
                --min-replicas)
                    min_replicas="$2"
                    shift 2
                    ;;
                --max-replicas)
                    max_replicas="$2"
                    shift 2
                    ;;
                --set-env-vars)
                    shift
                    while [ $# -gt 0 ] && [[ "$1" != --* ]]; do
                        env_vars+=("$1")
                        shift
                    done
                    ;;
                *)
                    if [[ "$1" == --* ]]; then
                        shift
                        if [ $# -gt 0 ] && [[ "$1" != --* ]]; then
                            shift
                        fi
                    else
                        shift
                    fi
                    ;;
            esac
        done

        local -a update_args=(--name "$app_name" --resource-group "$RESOURCE_GROUP")
        if [ -n "$image_val" ]; then
            update_args+=(--image "$image_val")
        fi
        if [ -n "$min_replicas" ]; then
            update_args+=(--min-replicas "$min_replicas")
        fi
        if [ -n "$max_replicas" ]; then
            update_args+=(--max-replicas "$max_replicas")
        fi
        if [ ${#env_vars[@]} -gt 0 ]; then
            update_args+=(--set-env-vars "${env_vars[@]}")
        fi

        az containerapp update "${update_args[@]}" --output none
    else
        echo "🚢 Creating $app_name..."
        local -a create_args=()
        local -a create_env_vars=()

        while [ $# -gt 0 ]; do
            case "$1" in
                --set-env-vars)
                    shift
                    while [ $# -gt 0 ] && [[ "$1" != --* ]]; do
                        create_env_vars+=("$1")
                        shift
                    done
                    ;;
                *)
                    local current_arg="$1"
                    create_args+=("$current_arg")
                    shift
                    if [ $# -gt 0 ] && [[ "$current_arg" == --* ]] && [[ "$1" != --* ]]; then
                        create_args+=("$1")
                        shift
                    fi
                    ;;
            esac
        done

        if [ ${#create_env_vars[@]} -gt 0 ]; then
            create_args+=(--env-vars "${create_env_vars[@]}")
        fi

        az containerapp create --name "$app_name" --resource-group "$RESOURCE_GROUP" "${create_args[@]}" --output none
    fi
}

create_or_update_ingress() {
    local app_name="$1"
    local target_port="$2"
    local ingress_type="$3"
    local transport="$4"

    local args=(--name "$app_name" --resource-group "$RESOURCE_GROUP" --target-port "$target_port" --type "$ingress_type")
    if [ -n "$transport" ]; then
        args+=(--transport "$transport")
    fi

    local current_ingress
    current_ingress="$(az containerapp show --name "$app_name" --resource-group "$RESOURCE_GROUP" --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null || true)"

    if [ -z "$current_ingress" ]; then
        az containerapp ingress enable "${args[@]}" --output none
    else
        az containerapp ingress update "${args[@]}" --output none
    fi
}

ACA_DOMAIN="$(az containerapp env show --name "$CAE_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.defaultDomain" -o tsv)"
USER_SERVICE_HOST="ca-user-service.internal.$ACA_DOMAIN"
DELIVERY_SERVICE_HOST="ca-delivery-service.internal.$ACA_DOMAIN"
VEHICLE_SERVICE_HOST="ca-vehicle-service.internal.$ACA_DOMAIN"
VEHICLE_SERVICE_GRPC_HOST="ca-vehicle-service-grpc.internal.$ACA_DOMAIN"
DRIVER_SERVICE_HOST="ca-driver-service.internal.$ACA_DOMAIN"
DRIVER_SERVICE_GRPC_HOST="ca-driver-service-grpc.internal.$ACA_DOMAIN"
ASSIGNMENT_SERVICE_HOST="ca-assignment-service.internal.$ACA_DOMAIN"
ROUTE_SERVICE_HOST="ca-route-service.internal.$ACA_DOMAIN"
TRACKING_SERVICE_HOST="ca-tracking-service.internal.$ACA_DOMAIN"

REGISTRY_ARGS=(
    --registry-server "$ACR_LOGIN_SERVER"
    --registry-username "$ACR_NAME"
    --registry-password "$ACR_PASSWORD"
)

SHARED_ENV=(
    "ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT"
    "Swagger__Enabled=true"
    "Jwt__Issuer=meridian-gateway"
    "Jwt__Audience=meridian-api"
    "Jwt__SecretKey=$JWT_SECRET"
    "Jwt__Secret=$JWT_SECRET"
)

GATEWAY_ENV=(
    "OCELOT_BASE_URL=https://placeholder"
    "Cors__AllowedOrigins__0=http://localhost:3000"
    "Cors__AllowedOrigins__1=https://jolly-hill-0547c9300.2.azurestaticapps.net"
    "USER_SERVICE_HOST=$USER_SERVICE_HOST"
    "DELIVERY_SERVICE_HOST=$DELIVERY_SERVICE_HOST"
    "VEHICLE_SERVICE_HOST=$VEHICLE_SERVICE_HOST"
    "DRIVER_SERVICE_HOST=$DRIVER_SERVICE_HOST"
    "ASSIGNMENT_SERVICE_HOST=$ASSIGNMENT_SERVICE_HOST"
    "ROUTE_SERVICE_HOST=$ROUTE_SERVICE_HOST"
    "TRACKING_SERVICE_HOST=$TRACKING_SERVICE_HOST"
)

create_or_update_app ca-api-gateway \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-apigateway:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars "${GATEWAY_ENV[@]}" "${SHARED_ENV[@]}"
create_or_update_ingress ca-api-gateway 8080 external ""

create_or_update_app ca-user-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-userservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__UserDb=$CONN_BASE;Initial Catalog=meridian_user;" \
        "Services__DriverServiceBaseUrl=https://$DRIVER_SERVICE_HOST" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-user-service 8080 internal ""

create_or_update_app ca-delivery-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-deliveryservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__DeliveryDb=$CONN_BASE;Initial Catalog=meridian_delivery;" \
        "Grpc__VehicleServiceUrl=https://$VEHICLE_SERVICE_GRPC_HOST" \
        "Swagger__ServerBasePath=/delivery" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-delivery-service 8080 internal auto

create_or_update_app ca-vehicle-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-vehicleservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__VehicleDb=$CONN_BASE;Initial Catalog=meridian_vehicle;" \
        "Reporting__DeliveryDatabaseName=meridian_delivery" \
        "Reporting__RouteDatabaseName=meridian_route" \
        "Swagger__ServerBasePath=/vehicle" \
        "ServiceMode=Rest" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-vehicle-service 8080 internal auto

create_or_update_app ca-vehicle-service-grpc \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-vehicleservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --target-port 8080 \
    --ingress internal \
    --transport http2 \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__VehicleDb=$CONN_BASE;Initial Catalog=meridian_vehicle;" \
        "Reporting__DeliveryDatabaseName=meridian_delivery" \
        "Reporting__RouteDatabaseName=meridian_route" \
        "ServiceMode=GrpcOnly" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-vehicle-service-grpc 8080 internal http2

create_or_update_app ca-driver-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-driverservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__DriverDb=$CONN_BASE;Initial Catalog=meridian_driver;" \
        "Reporting__DeliveryDatabaseName=meridian_delivery" \
        "Swagger__ServerBasePath=/driver" \
        "ServiceMode=Rest" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-driver-service 8080 internal auto

create_or_update_app ca-driver-service-grpc \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-driverservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --target-port 8080 \
    --ingress internal \
    --transport http2 \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__DriverDb=$CONN_BASE;Initial Catalog=meridian_driver;" \
        "Reporting__DeliveryDatabaseName=meridian_delivery" \
        "ServiceMode=GrpcOnly" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-driver-service-grpc 8080 internal http2

create_or_update_app ca-assignment-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-assignmentservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__AssignmentDb=$CONN_BASE;Initial Catalog=meridian_assignment;" \
        "Grpc__VehicleServiceUrl=https://$VEHICLE_SERVICE_GRPC_HOST" \
        "Grpc__DriverServiceUrl=https://$DRIVER_SERVICE_GRPC_HOST" \
        "Services__DeliveryServiceUrl=https://$DELIVERY_SERVICE_HOST" \
        "Swagger__ServerBasePath=/assignment" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-assignment-service 8080 internal auto

create_or_update_app ca-route-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-routeservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__RouteDb=$CONN_BASE;Initial Catalog=meridian_route;" \
        "ConnectionStrings__RedisCache=$REDIS_CONNECTION_STRING" \
        "Grpc__VehicleServiceUrl=https://$VEHICLE_SERVICE_GRPC_HOST" \
        "GoogleMaps__ApiKey=$GOOGLE_MAPS_API_KEY" \
        "Swagger__ServerBasePath=/route" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-route-service 8080 internal auto

create_or_update_app ca-tracking-service \
    --environment "$CAE_NAME" \
    --image "$ACR_LOGIN_SERVER/meridian-trackingservice:$IMAGE_TAG" \
    "${REGISTRY_ARGS[@]}" \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
        "ConnectionStrings__TrackingDb=$CONN_BASE;Initial Catalog=meridian_tracking;" \
        "Swagger__ServerBasePath=/tracking" \
        "${SHARED_ENV[@]}"
create_or_update_ingress ca-tracking-service 8080 internal auto

GATEWAY_FQDN="$(az containerapp show --resource-group "$RESOURCE_GROUP" --name ca-api-gateway --query "properties.configuration.ingress.fqdn" -o tsv)"
az containerapp update \
    --name ca-api-gateway \
    --resource-group "$RESOURCE_GROUP" \
    --set-env-vars "OCELOT_BASE_URL=https://$GATEWAY_FQDN" \
    --output none

echo ""
echo "✅ Meridian '$ENVIRONMENT' deployment complete."
echo "🌐 API Gateway: https://$GATEWAY_FQDN"
