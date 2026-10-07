# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props EquilibraFitPlusPlus.sln ./
COPY src/admin/EquilibraFitPlusPlus.Admin/EquilibraFitPlusPlus.Admin.csproj src/admin/EquilibraFitPlusPlus.Admin/
COPY src/backend/EquilibraFitPlusPlus.Contracts/EquilibraFitPlusPlus.Contracts.csproj src/backend/EquilibraFitPlusPlus.Contracts/
COPY src/backend/EquilibraFitPlusPlus.Shared/EquilibraFitPlusPlus.Shared.csproj src/backend/EquilibraFitPlusPlus.Shared/
RUN dotnet restore src/admin/EquilibraFitPlusPlus.Admin/EquilibraFitPlusPlus.Admin.csproj

COPY src/admin src/admin
COPY src/backend/EquilibraFitPlusPlus.Contracts src/backend/EquilibraFitPlusPlus.Contracts
COPY src/backend/EquilibraFitPlusPlus.Shared src/backend/EquilibraFitPlusPlus.Shared
RUN dotnet publish src/admin/EquilibraFitPlusPlus.Admin/EquilibraFitPlusPlus.Admin.csproj \
    --configuration $BUILD_CONFIGURATION \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080
EXPOSE 8080
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent "http://127.0.0.1:${PORT:-8080}/health" > /dev/null || exit 1
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://+:${PORT:-8080}; exec dotnet EquilibraFitPlusPlus.Admin.dll"]
