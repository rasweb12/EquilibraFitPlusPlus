#!/bin/sh
set -eu

export ASPNETCORE_URLS="http://+:${PORT:-8080}"
if [ "${RUN_DB_MIGRATIONS:-false}" = "true" ]; then
    connection="${SUPABASE_DB_CONNECTION_STRING:-${ConnectionStrings__Default:-}}"
    if [ -z "$connection" ]; then
        echo "Configure SUPABASE_DB_CONNECTION_STRING or ConnectionStrings__Default." >&2
        exit 1
    fi
    export DATABASE_PROVIDER=PostgreSQL
    ./efbundle --connection "$connection"
fi
exec dotnet EquilibraFitPlusPlus.Api.dll
