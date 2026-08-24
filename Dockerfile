# syntax=docker/dockerfile:1.7
# ============================================================================
# CageErpApi — Dockerfile multi-stage para deploy em VPS
# ============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FlyGates.Api/FlyGates.Api.csproj FlyGates.Api/
COPY FlyGates.Application/FlyGates.Application.csproj FlyGates.Application/
COPY FlyGates.Domain/FlyGates.Domain.csproj FlyGates.Domain/
COPY FlyGates.Infrastructure/FlyGates.Infrastructure.csproj FlyGates.Infrastructure/
COPY FlyGates.Repository/FlyGates.Repository.csproj FlyGates.Repository/

RUN dotnet restore FlyGates.Api/FlyGates.Api.csproj

COPY . .
RUN dotnet publish FlyGates.Api/FlyGates.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl tzdata ca-certificates \
 && rm -rf /var/lib/apt/lists/*

ENV TZ=America/Sao_Paulo \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8093 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false

COPY --from=build /app/publish .

EXPOSE 8093

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8093/health || exit 1

ENTRYPOINT ["dotnet", "FlyGates.Api.dll"]
