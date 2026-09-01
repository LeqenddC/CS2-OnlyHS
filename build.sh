#!/usr/bin/env bash
# Builds the plugin inside the .NET SDK container, so the host does not need dotnet installed.
# Output: build/out/  (copy its contents to addons/counterstrikesharp/plugins/CS2-OnlyHS/)
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p "$HOME/.nuget/packages"
docker run --rm -u "$(id -u):$(id -g)" \
  -v "$ROOT":/src -v "$HOME/.nuget/packages":/nuget -w /src \
  -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e NUGET_PACKAGES=/nuget \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e DOTNET_NOLOGO=1 \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet publish src/OnlyHS/CS2-OnlyHS.csproj -c Release -o build/out "$@"
