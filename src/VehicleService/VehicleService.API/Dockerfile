# Build context: Meridian-Backend/ (repo root)
# Run: docker build -t <image> -f src/VehicleService/VehicleService.API/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy shared proto files first (required for gRPC code generation)
COPY shared/ shared/

# Copy csproj and restore (layer-cached until csproj changes)
COPY src/VehicleService/VehicleService.API/VehicleService.API.csproj src/VehicleService/VehicleService.API/
RUN dotnet restore src/VehicleService/VehicleService.API/VehicleService.API.csproj

# Copy source and publish
COPY src/VehicleService/VehicleService.API/ src/VehicleService/VehicleService.API/
RUN dotnet publish src/VehicleService/VehicleService.API/VehicleService.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 6002
ENTRYPOINT ["dotnet", "VehicleService.API.dll"]
