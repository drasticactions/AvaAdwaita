#!/usr/bin/env bash
# Compiles libadwaita's stylesheet from external/libadwaita with the flags
# libadwaita's meson.build uses, into the bundled copy.
#
#   tools/build-stylesheet.sh           write src/AvaAdwaita.Theme/Sources/Bundled/gtk-1.10.0.css
#   tools/build-stylesheet.sh --check   fail if the bundled copy differs
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
src="$root/external/libadwaita/src/stylesheet"
out="$root/src/AvaAdwaita.Theme/Sources/Bundled/gtk-1.10.0.css"

command -v sassc >/dev/null || { echo "sassc is not installed" >&2; exit 1; }

tmp=$(mktemp)
trap 'rm -f "$tmp"' EXIT
(cd "$src" && sassc -a -M -t compact gtk.scss) > "$tmp"

if [ "${1:-}" = "--check" ]; then
    if ! cmp -s "$tmp" "$out"; then
        echo "the bundled stylesheet differs from sassc's output" >&2
        exit 1
    fi
    echo "bundled stylesheet is up to date"
    exit 0
fi

cp "$tmp" "$out"
cp "$src"/assets/*.svg "$root/src/AvaAdwaita.Theme/Sources/Bundled/assets/"
echo "wrote $out"
