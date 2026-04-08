# Azure Deployment Guide for Meridian

This guide outlines the steps required to deploy the Meridian microservices to Microsoft Azure. We use **Azure Container Apps** to host the services, **Azure Container Registry (ACR)** to store Docker images, **Azure SQL Database** as the managed relational database platform, and **Redis Cloud** for RouteService distributed caching.

Important deployment notes:

- All backend containers listen internally on port `8080` in Azure Container Apps.
- Local `appsettings.Development.json` files are for local development only and should not be baked into container images.
- Secrets such as JWT keys, database connection strings, Redis connection strings, and Google Maps API keys should be supplied through Container App environment variables or a secret store.

## Prerequisites

1.  **Azure Account:** An active Microsoft Azure account.
2.  **Azure CLI:** Installed on your local machine (`brew update && brew install azure-cli` on Mac).
3.  **Docker Desktop:** Running locally.

## Azure Acronym Legend

To keep resource names concise while adhering to naming standards, the following abbreviations (prefixes/suffixes) are used throughout the guide and in the deployment script:

*   **rg**: Resource Group
*   **acr**: Azure Container Registry
*   **cae**: Container Apps Environment
*   **ca**: Container App
*   **sqldb / sql-server**: Azure SQL Database / Server
*   **redis**: Redis cache endpoint
*   **kv**: Azure Key Vault
*   **law**: Log Analytics Workspace
*   **appi**: Application Insights

## Phase 1: Environment Topology

As per the architectural design, Meridian uses three distinct environments, each isolated in its own Azure Resource Group:

1.  **QA:** `rg-meridian-qa`
2.  **Staging:** `rg-meridian-staging`
3.  **Production:** `rg-meridian-prod`

Within each Resource Group, the following resources will be created:
*   **Azure Container Apps Environment:** The managed cluster hosting the microservices.
*   **Azure Container Apps:** The actual microservices (`ca-api-gateway`, `ca-user-service`, `ca-delivery-service`, `ca-vehicle-service`, `ca-driver-service`, `ca-assignment-service`, `ca-route-service`, `ca-tracking-service`).
*   **Azure SQL Database Server:** Hosting the relational databases (`meridian_user`, `meridian_delivery`, `meridian_vehicle`, `meridian_driver`, `meridian_assignment`, `meridian_route`, `meridian_tracking`).
*   **Redis Cloud database:** Used by RouteService for distributed route caching.
*   **Azure Key Vault:** Securely storing secrets (connection strings, JWT keys).
*   **Log Analytics Workspace & Application Insights:** For centralized logging and distributed tracing.

## Phase 2: Containerize the Microservices

Before deploying to Azure, create Docker images for all 8 components and push them to **Azure Container Registry (ACR)**.

### 1. Log in to ACR

```bash
ACR_NAME="acrmeridian<env>"  # e.g. acrmeridianqa
az acr login --name $ACR_NAME
```

### 2. Build and push the Docker Images

Ensure you have a `Dockerfile` for each API project. Then build and push images:

```bash
# Run all commands from the Meridian-Backend/ directory.
# Services that depend on shared gRPC protos use `.` (repo root) as the build context.
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer -o tsv)

# 1. API Gateway (build context: service directory)
docker build -t $ACR_LOGIN_SERVER/meridian-apigateway:v1 -f src/ApiGateway/Dockerfile src/ApiGateway
docker push $ACR_LOGIN_SERVER/meridian-apigateway:v1

# 2. User Service (build context: service directory)
docker build -t $ACR_LOGIN_SERVER/meridian-userservice:v1 -f src/UserService/UserService.API/Dockerfile src/UserService/UserService.API
docker push $ACR_LOGIN_SERVER/meridian-userservice:v1

# 3. Delivery Service (build context: repo root — needs shared/protos/vehicle.proto)
docker build -t $ACR_LOGIN_SERVER/meridian-deliveryservice:v1 -f src/DeliveryService/DeliveryService.API/Dockerfile .
docker push $ACR_LOGIN_SERVER/meridian-deliveryservice:v1

# 4. Vehicle Service (build context: repo root — needs shared/protos/vehicle.proto)
docker build -t $ACR_LOGIN_SERVER/meridian-vehicleservice:v1 -f src/VehicleService/VehicleService.API/Dockerfile .
docker push $ACR_LOGIN_SERVER/meridian-vehicleservice:v1

# 5. Driver Service (build context: repo root — needs shared/protos/driver.proto)
docker build -t $ACR_LOGIN_SERVER/meridian-driverservice:v1 -f src/DriverService/DriverService.API/Dockerfile .
docker push $ACR_LOGIN_SERVER/meridian-driverservice:v1

# 6. Assignment Service (build context: repo root — needs shared/protos/vehicle.proto + driver.proto)
docker build -t $ACR_LOGIN_SERVER/meridian-assignmentservice:v1 -f src/AssignmentService/AssignmentService.API/Dockerfile .
docker push $ACR_LOGIN_SERVER/meridian-assignmentservice:v1

# 7. Route Service (build context: repo root — needs shared/protos/vehicle.proto)
docker build -t $ACR_LOGIN_SERVER/meridian-routeservice:v1 -f src/RouteService/RouteService.API/Dockerfile .
docker push $ACR_LOGIN_SERVER/meridian-routeservice:v1

# 8. Tracking Service (build context: service directory)
docker build -t $ACR_LOGIN_SERVER/meridian-trackingservice:v1 -f src/TrackingService/TrackingService.API/Dockerfile src/TrackingService/TrackingService.API
docker push $ACR_LOGIN_SERVER/meridian-trackingservice:v1
```

## Phase 3: Provision Azure Resources (Batch Script)

To keep the deployment clean and automated, use the following bash script to provision the reusable Azure resources for a specific environment. The SQL logical server is created by automation, but the seven application databases are created manually ahead of time. Redis is provided by Redis Cloud and injected into RouteService as an environment variable.

> **Note:** Ensure you are logged in using `az login` before running this script.

```bash
#!/bin/bash

# ==============================================================================
# Meridian Environment Infrastructure Setup Script
# ==============================================================================

# Configuration Variables
ENV="qa" # Change to 'staging' or 'prod' as needed
LOCATION="eastasia"

# Resource Names (Following naming conventions)
RESOURCE_GROUP="rg-meridian-$ENV"
SQL_SERVER="sql-meridian-${ENV}001"
CAE_NAME="cae-meridian-$ENV"

# Database Configuration (Update these securely)
DB_ADMIN="meridianadmin"
DB_PASSWORD="Passw0rd!"

# Azure Container Registry
ACR_NAME="acrmeridian$ENV"
ACR_LOGIN_SERVER="$ACR_NAME.azurecr.io"

echo "🚀 Deploying Meridian Platform ($ENV Environment)..."

# 1. Create Resource Group
echo "📦 Creating Resource Group: $RESOURCE_GROUP"
az group create --name $RESOURCE_GROUP --location $LOCATION

# 2. Create Azure SQL Server and configure firewall
echo "🗄️ Creating SQL Server: $SQL_SERVER..."
az sql server create --name $SQL_SERVER --resource-group $RESOURCE_GROUP --location $LOCATION --admin-user $DB_ADMIN --admin-password $DB_PASSWORD
az sql server firewall-rule create --resource-group $RESOURCE_GROUP --server $SQL_SERVER --name AllowAllAzureIPs --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# Manually create these seven databases on the logical server before deployment:
# meridian_user, meridian_delivery, meridian_vehicle, meridian_driver,
# meridian_assignment, meridian_route, meridian_tracking

# 3. Create Azure Container Registry
echo "📦 Creating Azure Container Registry: $ACR_NAME..."
az acr create --name $ACR_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --sku Basic --admin-enabled true
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query passwords[0].value -o tsv)

# 4. Create Container Apps Environment
echo "☁️ Creating Container Apps Environment: $CAE_NAME..."
az containerapp env create --name $CAE_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --logs-destination none

# 5. Create Container Apps (Microservices)
echo "🛳️ Deploying Microservices to Container Apps..."

# API Gateway (Publicly accessible ingress, container listens on 8080)
az containerapp create \
    --name ca-api-gateway \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-apigateway:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress external \
    --min-replicas 1 \
    --max-replicas 3

# User Service (Internal ingress, container listens on 8080)
az containerapp create \
    --name ca-user-service \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-userservice:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress internal \
    --min-replicas 0 \
    --max-replicas 5

# Delivery Service (Internal ingress, container listens on 8080)
az containerapp create \
    --name ca-delivery-service \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-deliveryservice:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress internal \
    --transport http2 \
    --min-replicas 0 \
    --max-replicas 5

# Vehicle Service (Internal ingress, container listens on 8080)
az containerapp create \
    --name ca-vehicle-service \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-vehicleservice:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress internal \
    --transport http2 \
    --min-replicas 0 \
    --max-replicas 5

# Driver Service (Internal ingress, container listens on 8080)
az containerapp create \
    --name ca-driver-service \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-driverservice:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress internal \
    --transport http2 \
    --min-replicas 0 \
    --max-replicas 5

# Assignment Service (Internal ingress, container listens on 8080)
az containerapp create \
    --name ca-assignment-service \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-assignmentservice:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress internal \
    --transport http2 \
    --min-replicas 0 \
    --max-replicas 5

# Route Service (Internal ingress, container listens on 8080)
az containerapp create \
    --name ca-route-service \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-routeservice:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress internal \
    --transport http2 \
    --min-replicas 0 \
    --max-replicas 5

# Tracking Service (Internal ingress, container listens on 8080; SignalR uses auto transport)
az containerapp create \
    --name ca-tracking-service \
    --resource-group $RESOURCE_GROUP \
    --environment $CAE_NAME \
    --image $ACR_LOGIN_SERVER/meridian-trackingservice:v1 \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_NAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress internal \
    --transport auto \
    --min-replicas 0 \
    --max-replicas 5

echo "✅ Environment setup complete! 
API Gateway URL: $(az containerapp show --resource-group $RESOURCE_GROUP --name ca-api-gateway --query properties.configuration.ingress.fqdn -o tsv)"
```

## Phase 4: CI/CD Pipeline Automation

Once manual deployment is confirmed via this script, configure GitHub Actions to automatically run on code commit. This entails:
1. Authenticating to Azure from GitHub Actions using OIDC via `azure/login`.
2. Building and pushing images to ACR (`$ACR_LOGIN_SERVER`) on every commit.
3. Creating or updating the Azure resource group, SQL logical server, ACR, and Container Apps environment from the workflow.
4. Reusing the manually created SQL databases during deployment.
5. Injecting the Redis Cloud endpoint and secret into RouteService during deployment.

## Swagger in Azure

The API Gateway does not host a standalone aggregated Swagger UI. Instead, it proxies each service's Swagger endpoints. After deployment, Swagger should be reachable through the gateway FQDN on paths such as:

- `/delivery/swagger`
- `/vehicle/swagger`
- `/driver/swagger`
- `/assignment/swagger`
- `/route/swagger`
- `/tracking/swagger`
- `/user/swagger`
