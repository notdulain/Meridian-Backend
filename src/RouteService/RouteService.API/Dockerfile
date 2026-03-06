# Build context: Meridian-Backend/ (repo root)
# Run: docker build -t <image> -f src/RouteService/RouteService.API/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy shared proto files first (required for gRPC code generation)
COPY shared/ shared/

# Copy csproj and restore (layer-cached until csproj changes)
COPY src/RouteService/RouteService.API/RouteService.API.csproj src/RouteService/RouteService.API/
RUN dotnet restore src/RouteService/RouteService.API/RouteService.API.csproj

# Copy source and publish
COPY src/RouteService/RouteService.API/ src/RouteService/RouteService.API/
RUN dotnet publish src/RouteService/RouteService.API/RouteService.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 6005
ENTRYPOINT ["dotnet", "RouteService.API.dll"]
