# Build context: Meridian-Backend/ (repo root)
# Run: docker build -t <image> -f src/DriverService/DriverService.API/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy shared proto files first (required for gRPC code generation)
COPY shared/ shared/

# Copy csproj and restore (layer-cached until csproj changes)
COPY src/DriverService/DriverService.API/DriverService.API.csproj src/DriverService/DriverService.API/
RUN dotnet restore src/DriverService/DriverService.API/DriverService.API.csproj

# Copy source and publish
COPY src/DriverService/DriverService.API/ src/DriverService/DriverService.API/
RUN dotnet publish src/DriverService/DriverService.API/DriverService.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 6003
ENTRYPOINT ["dotnet", "DriverService.API.dll"]
