#!/usr/bin/env bash
# Plunderspell headless verification: compile every gameplay assembly and run the whole test suite
# without the Unity editor. Exits non-zero on the first failure, so it works as a CI gate.
#
#   ./Tools/Headless/verify.sh            build + test
#   ./Tools/Headless/verify.sh --build    build only
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Find a dotnet SDK: PATH first, then the usual install locations.
if command -v dotnet >/dev/null 2>&1; then
  DOTNET="$(command -v dotnet)"
elif [ -x /opt/dotnet/dotnet ]; then
  DOTNET=/opt/dotnet/dotnet
elif [ -x "$HOME/.dotnet/dotnet" ]; then
  DOTNET="$HOME/.dotnet/dotnet"
else
  echo "error: no dotnet SDK found. Install .NET 8:" >&2
  echo "  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir /opt/dotnet" >&2
  exit 127
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

echo "==> Building gameplay assemblies (headless)"
"$DOTNET" build "$HERE/Plunderspell.Headless/Plunderspell.Headless.csproj" -v minimal --nologo

echo "==> Building editor tooling (headless)"
"$DOTNET" build "$HERE/Plunderspell.Headless.Editor/Plunderspell.Headless.Editor.csproj" -v minimal --nologo

if [ "${1:-}" = "--build" ]; then
  echo "==> Build only; skipping tests."
  exit 0
fi

echo "==> Running test suite"
"$DOTNET" test "$HERE/Plunderspell.Headless.Tests/Plunderspell.Headless.Tests.csproj" -v minimal --nologo

echo "==> OK: build clean, all tests green."
