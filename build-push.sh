#!/bin/bash

# ==============================================================================
# Meridian - Build & Push All Services to ACR
# Run from: Meridian-Backend/
#
# Uses local docker buildx to produce linux/amd64 images and push them to ACR.
# This avoids Azure Container Registry Tasks, which may be restricted on student
# subscriptions, while still producing images that Azure Container Apps can run.
# ==============================================================================

set -euo pipefail

ACR_NAME="${ACR_NAME:-acrmeridianqa}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-meridian-qa}"
LOCATION="${LOCATION:-eastasia}"
TAG="${TAG:-v1}"

ensure_command() {
    local command_name="$1"
    if ! command -v "$command_name" >/dev/null 2>&1; then
        echo "❌ Required command not found: $command_name" >&2
        exit 1
    fi
}

ensure_acr() {
    echo "📦 Ensuring Resource Group exists: $RESOURCE_GROUP"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION" >/dev/null

    if az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
        echo "⏭️  ACR '$ACR_NAME' already exists, skipping creation."
    else
        echo "📦 Creating Azure Container Registry: $ACR_NAME..."
        az acr create \
            --name "$ACR_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --sku Basic \
            --admin-enabled true \
            --tags Project=Meridian Environment=QA >/dev/null
    fi

    az acr update --name "$ACR_NAME" --admin-enabled true >/dev/null
    ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)
}

ensure_builder() {
    if ! docker buildx inspect >/dev/null 2>&1; then
        echo "🔧 Creating docker buildx builder..."
        docker buildx create --name meridian-builder --use >/dev/null
    fi
}

build_and_push() {
    local service_name="$1"
    local image_name="$2"
    local dockerfile_path="$3"
    local build_context="$4"

    echo "☁️  Building $service_name (linux/amd64) and pushing to ACR..."
    docker buildx build \
        --platform linux/amd64 \
        --tag "$ACR_LOGIN_SERVER/$image_name:$TAG" \
        --file "$dockerfile_path" \
        --push \
        "$build_context"
    echo "✅ $service_name pushed"
}

ensure_command az
ensure_command docker

if ! docker buildx version >/dev/null 2>&1; then
    echo "❌ docker buildx is required." >&2
    exit 1
fi

ensure_acr

echo "🔐 Logging in to ACR: $ACR_NAME"
az acr login --name "$ACR_NAME"

ensure_builder

build_and_push "API Gateway" "meridian-apigateway" "src/ApiGateway/Dockerfile" "src/ApiGateway"
build_and_push "User Service" "meridian-userservice" "src/UserService/UserService.API/Dockerfile" "src/UserService/UserService.API"
build_and_push "Delivery Service" "meridian-deliveryservice" "src/DeliveryService/DeliveryService.API/Dockerfile" "."
build_and_push "Vehicle Service" "meridian-vehicleservice" "src/VehicleService/VehicleService.API/Dockerfile" "."
build_and_push "Driver Service" "meridian-driverservice" "src/DriverService/DriverService.API/Dockerfile" "."
build_and_push "Assignment Service" "meridian-assignmentservice" "src/AssignmentService/AssignmentService.API/Dockerfile" "."
build_and_push "Route Service" "meridian-routeservice" "src/RouteService/RouteService.API/Dockerfile" "."
build_and_push "Tracking Service" "meridian-trackingservice" "src/TrackingService/TrackingService.API/Dockerfile" "src/TrackingService/TrackingService.API"

echo ""
echo "🎉 All services built and pushed to $ACR_LOGIN_SERVER (tag: $TAG)"
