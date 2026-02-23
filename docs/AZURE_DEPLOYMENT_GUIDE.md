# Azure Deployment Guide for Meridian

This guide outlines the steps required to deploy the Meridian microservices to Microsoft Azure. We will be using **Azure App Service (Web App for Containers)** to host our Dockerized microservices and **Azure SQL Database** as our managed relational database.

## Prerequisites

1.  **Azure Account:** An active Microsoft Azure account.
2.  **Azure CLI:** Installed on your local machine (`brew update && brew install azure-cli` on Mac).
3.  **Docker Desktop:** Running locally.
4.  **Docker Hub or Azure Container Registry (ACR):** An account to host your Docker images.

## Phase 1: Containerize the Microservices

Before deploying to Azure, we need to create a Docker image for each service.

### 1. Create Dockerfiles

We need a `Dockerfile` in the root of each API project.
*   `src/DeliveryService/DeliveryService.API/Dockerfile`
*   `src/FleetService/FleetService.API/Dockerfile`
*   `src/RouteService/RouteService.API/Dockerfile`

Here is the standard `.NET 10` Dockerfile template you should place in each of those locations (make sure to replace `DeliveryService.API.csproj` and `DeliveryService.API.dll` with the respective service names):

```dockerfile
# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DeliveryService.API.csproj", "./"]
RUN dotnet restore "./DeliveryService.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DeliveryService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeliveryService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeliveryService.API.dll"]
```

### 2. Build the Docker Images

From the directory containing each `Dockerfile`, run the build command. Let's tag them with your Docker Hub username (e.g., `notdulain`).

```bash
docker build -t notdulain/meridian-delivery:v1 -f src/DeliveryService/DeliveryService.API/Dockerfile src/DeliveryService/DeliveryService.API
docker build -t notdulain/meridian-fleet:v1 -f src/FleetService/FleetService.API/Dockerfile src/FleetService/FleetService.API
docker build -t notdulain/meridian-route:v1 -f src/RouteService/RouteService.API/Dockerfile src/RouteService/RouteService.API
```

### 3. Push to Container Registry

Log in to Docker Hub and push your images.

```bash
docker login
docker push notdulain/meridian-delivery:v1
docker push notdulain/meridian-fleet:v1
docker push notdulain/meridian-route:v1
```

## Phase 2: Provision Azure Resources (via CLI)

We will use the Azure CLI to create the necessary resources. Open your terminal and log in:

```bash
az login
```

### 1. Environment Variables

Let's set some variables to make the commands easier to copy-paste. Replace `notdulain` with your actual Azure subscription ID if needed, and choose a region close to you.

```bash
RESOURCE_GROUP="rg-meridian-prod"
LOCATION="eastus"
SQL_SERVER="sql-meridian-prod-$$RANDOM" # Randomizes name to be unique
DB_ADMIN="meridianadmin"
DB_PASSWORD="YourStrong!Passw0rd123"
APP_SERVICE_PLAN="asp-meridian-prod"
```

### 2. Create a Resource Group

This groups all our Meridian resources together.

```bash
az group create --name $RESOURCE_GROUP --location $LOCATION
```

### 3. Provision Azure SQL Database

We will deploy a single logical SQL server, and then the `.NET` DbUp migrations will create the specific databases (`delivery_prod`, etc.) when the apps start up.

```bash
# Create the SQL Server
az sql server create \
    --name $SQL_SERVER \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --admin-user $DB_ADMIN \
    --admin-password $DB_PASSWORD

# Create a firewall rule to allow all Azure internal IPs to access the database
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER \
    --name AllowAllAzureIPs \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0

# Optional: Allow your local machine's IP if you want to connect via SSMS/Azure Data Studio
# az sql server firewall-rule create --resource-group $RESOURCE_GROUP --server $SQL_SERVER --name AllowMyIP --start-ip-address <YOUR_IP> --end-ip-address <YOUR_IP>
```

Keep the full Server Name handy (e.g., `sql-meridian-prod-1234.database.windows.net`). Your Production Connection String will look like this:
`Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=delivery_prod;User ID=$DB_ADMIN;Password=$DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`

### 4. Setup Redis Cache (For Route Service)

```bash
az redis create \
    --name redis-meridian-prod \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Basic \
    --vm-size c0
```
*Note: This takes 15-20 minutes to provision.*

### 5. Create App Service Plan (Linux)

This is the underlying compute infrastructure. A `B1` (Basic) plan is good for development/testing real workloads.

```bash
az appservice plan create \
    --name $APP_SERVICE_PLAN \
    --resource-group $RESOURCE_GROUP \
    --sku B1 \
    --is-linux
```

## Phase 3: Deploy the Web Apps

Now we deploy the Docker containers to Azure App Service.

### 1. Create Web Apps & Link Containers

```bash
# Delivery Service
az webapp create \
    --resource-group $RESOURCE_GROUP \
    --plan $APP_SERVICE_PLAN \
    --name app-meridian-delivery \
    --deployment-container-image-name notdulain/meridian-delivery:v1

# Fleet Service
az webapp create \
    --resource-group $RESOURCE_GROUP \
    --plan $APP_SERVICE_PLAN \
    --name app-meridian-fleet \
    --deployment-container-image-name notdulain/meridian-fleet:v1

# Route Service
az webapp create \
    --resource-group $RESOURCE_GROUP \
    --plan $APP_SERVICE_PLAN \
    --name app-meridian-route \
    --deployment-container-image-name notdulain/meridian-route:v1
```

### 2. Configure Environment Variables (App Settings)

We need to inject our production connection strings and JWT secrets. DbUp will automatically run against these new databases on startup.

**Crucial Step:** When defining the connection string in App Settings, we must override the exact structure in `appsettings.json`.

```bash
# Example for Delivery Service
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name app-meridian-delivery \
    --settings \
    ConnectionStrings__DeliveryDb="Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=delivery_prod;User ID=$DB_ADMIN;Password=$DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    Jwt__SecretKey="YOUR_VERY_SECURE_PRODUCTION_LONG_KEY_HERE!" \
    Jwt__Issuer="MeridianProduction" \
    Jwt__Audience="MeridianClients" \
    WEBSITES_PORT=8080 # Tells Azure which port the container exposes
```

*Repeat the `az webapp config appsettings set` command for Fleet and Route services, updating the `ConnectionStrings__FleetDb` and `ConnectionStrings__RouteDb` keys respectively. For the Route service, also add the Redis connection string key!*

### 3. Restart and Verify

```bash
az webapp restart --name app-meridian-delivery --resource-group $RESOURCE_GROUP
```

Navigate to `https://app-meridian-delivery.azurewebsites.net/openapi/v1.json` to verify the deployment is live!

---
## Next Steps (Advanced)
Once manual deployment is working, the next logical step is to automate this process by creating a **GitHub Actions Workflow** (`.github/workflows/deploy.yml`) that builds the Docker image and pushes it to Azure automatically whenever code is merged into the `main` branch.
