# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props EquilibraFitPlusPlus.sln ./
COPY src/backend/EquilibraFitPlusPlus.Api/EquilibraFitPlusPlus.Api.csproj src/backend/EquilibraFitPlusPlus.Api/
COPY src/backend/EquilibraFitPlusPlus.Application/EquilibraFitPlusPlus.Application.csproj src/backend/EquilibraFitPlusPlus.Application/
COPY src/backend/EquilibraFitPlusPlus.Contracts/EquilibraFitPlusPlus.Contracts.csproj src/backend/EquilibraFitPlusPlus.Contracts/
COPY src/backend/EquilibraFitPlusPlus.Domain/EquilibraFitPlusPlus.Domain.csproj src/backend/EquilibraFitPlusPlus.Domain/
COPY src/backend/EquilibraFitPlusPlus.Infrastructure/EquilibraFitPlusPlus.Infrastructure.csproj src/backend/EquilibraFitPlusPlus.Infrastructure/
COPY src/backend/EquilibraFitPlusPlus.Shared/EquilibraFitPlusPlus.Shared.csproj src/backend/EquilibraFitPlusPlus.Shared/
RUN dotnet restore src/backend/EquilibraFitPlusPlus.Api/EquilibraFitPlusPlus.Api.csproj

COPY src/backend src/backend
RUN dotnet publish src/backend/EquilibraFitPlusPlus.Api/EquilibraFitPlusPlus.Api.csproj \
    --configuration $BUILD_CONFIGURATION \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.5 \
    && ConnectionStrings__Default='Host=localhost;Database=design_time_only' /tools/dotnet-ef migrations bundle \
        --project src/backend/EquilibraFitPlusPlus.Infrastructure/EquilibraFitPlusPlus.Infrastructure.csproj \
        --startup-project src/backend/EquilibraFitPlusPlus.Api/EquilibraFitPlusPlus.Api.csproj \
        --configuration $BUILD_CONFIGURATION \
        --no-build \
        --output /app/migrations/efbundle \
        --force

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080 \
    RUN_DB_MIGRATIONS=false
EXPOSE 8080
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY --from=build /app/migrations/efbundle ./efbundle
COPY src/backend/EquilibraFitPlusPlus.Api/entrypoint.sh ./entrypoint.sh
RUN sed -i 's/\r$//' entrypoint.sh && chmod +x entrypoint.sh efbundle
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent "http://127.0.0.1:${PORT:-8080}/health/ready" > /dev/null || exit 1
ENTRYPOINT ["sh", "/app/entrypoint.sh"]
