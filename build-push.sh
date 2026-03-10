#!/bin/bash

# ==============================================================================
# Meridian - Build & Push All Services to ACR
# Run from: Meridian-Backend/
#
# Uses `az acr build` (cloud build on Azure) instead of local docker build.
# This avoids the QEMU cross-compilation slowness on Apple Silicon Macs —
# Azure builds natively on linux/amd64. No docker push needed separately.
# ==============================================================================

set -e

ACR_NAME="${ACR_NAME:?ACR_NAME env var is required (e.g. acrmeridianqa)}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-meridian-qa}"
LOCATION="${LOCATION:-eastasia}"
TAG="${TAG:-v1}"

# ---------- Provision Resource Group (idempotent) ----------
echo "📦 Ensuring Resource Group exists: $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION"

# ---------- Provision ACR (idempotent) ----------
echo "📦 Ensuring ACR exists: $ACR_NAME"
az acr create --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" \
    --location "$LOCATION" --sku Basic --admin-enabled true \
    --tags Project=Meridian Environment=QA 2>/dev/null \
    || echo "   ACR already exists, continuing..."
az acr update --name "$ACR_NAME" --admin-enabled true

# ---------- 1. API Gateway ----------
echo "☁️  Building API Gateway on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-apigateway:$TAG" \
    --platform linux/amd64 \
    --file src/ApiGateway/Dockerfile \
    src/ApiGateway
echo "✅ API Gateway pushed"

# ---------- 2. User Service ----------
echo "☁️  Building User Service on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-userservice:$TAG" \
    --platform linux/amd64 \
    --file src/UserService/UserService.API/Dockerfile \
    src/UserService/UserService.API
echo "✅ User Service pushed"

# ---------- 3. Delivery Service ----------
echo "☁️  Building Delivery Service on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-deliveryservice:$TAG" \
    --platform linux/amd64 \
    --file src/DeliveryService/DeliveryService.API/Dockerfile \
    src/DeliveryService/DeliveryService.API
echo "✅ Delivery Service pushed"

# ---------- 4. Vehicle Service (repo root context — needs shared/protos) ----------
echo "☁️  Building Vehicle Service on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-vehicleservice:$TAG" \
    --platform linux/amd64 \
    --file src/VehicleService/VehicleService.API/Dockerfile \
    .
echo "✅ Vehicle Service pushed"

# ---------- 5. Driver Service (repo root context — needs shared/protos) ----------
echo "☁️  Building Driver Service on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-driverservice:$TAG" \
    --platform linux/amd64 \
    --file src/DriverService/DriverService.API/Dockerfile \
    .
echo "✅ Driver Service pushed"

# ---------- 6. Assignment Service (repo root context — needs shared/protos) ----------
echo "☁️  Building Assignment Service on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-assignmentservice:$TAG" \
    --platform linux/amd64 \
    --file src/AssignmentService/AssignmentService.API/Dockerfile \
    .
echo "✅ Assignment Service pushed"

# ---------- 7. Route Service (repo root context — needs shared/protos) ----------
echo "☁️  Building Route Service on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-routeservice:$TAG" \
    --platform linux/amd64 \
    --file src/RouteService/RouteService.API/Dockerfile \
    .
echo "✅ Route Service pushed"

# ---------- 8. Tracking Service ----------
echo "☁️  Building Tracking Service on ACR (linux/amd64)..."
az acr build \
    --registry "$ACR_NAME" \
    --image "meridian-trackingservice:$TAG" \
    --platform linux/amd64 \
    --file src/TrackingService/TrackingService.API/Dockerfile \
    src/TrackingService/TrackingService.API
echo "✅ Tracking Service pushed"

echo ""
echo "🎉 All services built and pushed to $ACR_NAME (tag: $TAG)"
