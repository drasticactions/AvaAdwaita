#!/usr/bin/env bash
# Renders every gallery page headless, light and dark, to artifacts/gallery/{light,dark}/.
# Extra arguments go to the gallery (--accent purple, --high-contrast).
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
out="$root/artifacts/gallery"
project="$root/samples/AvaAdwaita.Gallery.Desktop"

dotnet build "$project" --nologo -v quiet
dotnet run --project "$project" --no-build -- --shoot "$out/light" "$@"
dotnet run --project "$project" --no-build -- --shoot "$out/dark" --dark "$@"
