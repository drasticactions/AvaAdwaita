#!/usr/bin/env bash
# Regenerates the parity data from GTK: builds tools/adw-oracle against the installed
# GTK and libadwaita, writes tests/Data/parity/corpus.json from its generator, and runs
# the oracle under a headless Weston to write expected-gtk.json and the goldens, then
# under a second Weston whose output is at scale 2 for the scale-2 goldens.
#
#   --check         fail if the checked-in data differs (CI on main)
#   --only PREFIX   regenerate only the scenes whose id starts with PREFIX (or any of
#                   several, comma-separated) and merge them
#                   into the checked-in data, for working on one control
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
check=0
only=""
case "${1:-}" in
    --check) check=1 ;;
    --only) only="${2:?--only needs a scene id prefix}" ;;
esac

build="$root/artifacts/adw-oracle"
mkdir -p "$build"
make -C "$root/tools/adw-oracle" OUT="$build" --no-print-directory

data="$root/tests/Data"
work="$build/data"
rm -rf "${work:?}"
mkdir -p "$work/parity" "$work/goldens/scale2"

python3 "$root/tools/adw-oracle/corpus.py" > "$work/parity/corpus.json"
oracle_corpus="$work/parity/corpus.json"
if [ -n "$only" ]; then
    oracle_corpus="$work/only.json"
    python3 - "$work/parity/corpus.json" "$oracle_corpus" "$only" <<'PY'
import json, sys
prefixes = tuple(p for p in sys.argv[3].split(",") if p)
entries = [e for e in json.load(open(sys.argv[1])) if e["id"].startswith(prefixes)]
json.dump(entries, open(sys.argv[2], "w"))
print(len(entries), "scenes", file=sys.stderr)
PY
fi

runtime="${XDG_RUNTIME_DIR:-/run/user/$(id -u)}"
pids=()
trap 'kill "${pids[@]}" 2>/dev/null || true' EXIT

# Starts a headless Weston with extra options; its socket name is left in $socket.
start_weston() {
    socket="adw-oracle-$$-$1"
    shift
    # The pixman renderer: the headless one without a renderer crashes when GTK opens a popup.
    weston --backend=headless --renderer=pixman --socket="$socket" --idle-time=0 "$@" >/dev/null 2>&1 &
    pids+=($!)
    for _ in $(seq 50); do
        if [ -S "$runtime/$socket" ]; then
            break
        fi
        sleep 0.1
    done
}

if [ -z "${WAYLAND_DISPLAY:-}" ]; then
    start_weston 1
    export WAYLAND_DISPLAY="$socket"
fi
start_weston 2 --scale=2
scale2="$socket"

# Schema defaults for every GSettings key, no settings portal and no settings.ini, so the host's
# fonts, icon theme and other settings do not leak in.
config="$build/empty-config"
mkdir -p "$config"
export GSETTINGS_BACKEND=memory GDK_DEBUG=no-portals XDG_CONFIG_HOME="$config"
GDK_BACKEND=wayland GSK_RENDERER=cairo "$build/adw-oracle" \
    --corpus "$oracle_corpus" \
    --out "$work/parity/expected-gtk.json" \
    --goldens "$work/goldens"
WAYLAND_DISPLAY="$scale2" GDK_BACKEND=wayland GSK_RENDERER=cairo "$build/adw-oracle" \
    --corpus "$oracle_corpus" \
    --out "$work/scale2.json" \
    --goldens "$work/goldens/scale2" --scale 2

if [ -n "$only" ]; then
    # Merge the scenes into the checked-in data, in the corpus's order.
    python3 - "$work/parity" "$data/parity" "$root/tools/adw-oracle" <<'PY'
import json, sys
work, data = sys.argv[1], sys.argv[2]
sys.path.insert(0, sys.argv[3])
import jsonglib
corpus = json.load(open(f"{work}/corpus.json"))
merged = json.load(open(f"{data}/expected-gtk.json"))
merged.update(json.load(open(f"{work}/expected-gtk.json")))
ordered = {e["id"]: merged[e["id"]] for e in corpus if e["id"] in merged}
open(f"{data}/expected-gtk.json", "w").write(jsonglib.dumps(ordered))
with open(f"{data}/corpus.json", "w") as f:
    json.dump(corpus, f, indent=1)
    f.write("\n")
PY
    cp "$work/goldens/"*.png "$data/goldens/"
    cp "$work/goldens/scale2/"*.png "$data/goldens/scale2/"
    echo "merged the $only scenes into $data"
elif [ "$check" -eq 1 ]; then
    diff -r -q -x known-differences.txt "$work/parity" "$data/parity"
    # A spinner turns, so its goldens show a different moment each run; the tests match whatever moment it is.
    diff -r -q -x known-differences.txt -x 'spinner-*' "$work/goldens" "$data/goldens"
    echo "parity data is up to date"
else
    mkdir -p "$data/parity" "$data/goldens/scale2"
    cp "$work/parity/"*.json "$data/parity/"
    find "$data/goldens" -name '*.png' -delete
    cp "$work/goldens/"*.png "$data/goldens/"
    cp "$work/goldens/scale2/"*.png "$data/goldens/scale2/"
    echo "wrote $data/parity and $data/goldens"
fi
