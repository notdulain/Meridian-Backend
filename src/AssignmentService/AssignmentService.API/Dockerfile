# Build context: Meridian-Backend/ (repo root)
# Run: docker build -t <image> -f src/AssignmentService/AssignmentService.API/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy shared proto files first (required for gRPC code generation)
COPY shared/ shared/

# Copy csproj and restore (layer-cached until csproj changes)
COPY src/AssignmentService/AssignmentService.API/AssignmentService.API.csproj src/AssignmentService/AssignmentService.API/
RUN dotnet restore src/AssignmentService/AssignmentService.API/AssignmentService.API.csproj

# Copy source and publish
COPY src/AssignmentService/AssignmentService.API/ src/AssignmentService/AssignmentService.API/
RUN dotnet publish src/AssignmentService/AssignmentService.API/AssignmentService.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AssignmentService.API.dll"]
