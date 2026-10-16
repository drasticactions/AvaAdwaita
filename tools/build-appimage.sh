#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat <<'TEXT'
Usage: tools/build-appimage.sh [options]

Publishes the desktop gallery with NativeAOT and packs it as an AppImage in
artifacts/appimage/. The AppImage carries the bundled libadwaita stylesheet,
icons and fonts. It uses the system's libadwaita stylesheet, GTK translations
and desktop settings where the system has them.

Options:
  --rid <rid>        linux-x64 or linux-arm64. The default is the RID of this
                     computer. NativeAOT builds only for the RID of this
                     computer.
  --revision <n>     The fourth number of the version. The default is 0.
  --out <dir>        The output folder. The default is artifacts/appimage.
TEXT
}

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APPIMAGETOOL_VERSION=1.9.1

case "$(uname -m)" in
    x86_64) HOST_RID=linux-x64 ;;
    aarch64) HOST_RID=linux-arm64 ;;
    *) echo "no AppImage for $(uname -m)" >&2; exit 1 ;;
esac

RID=$HOST_RID
REVISION=0
OUT="$ROOT/artifacts/appimage"

while [ $# -gt 0 ]; do
    case "$1" in
        --rid) RID=${2:?}; shift 2 ;;
        --revision) REVISION=${2:?}; shift 2 ;;
        --out) OUT=${2:?}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; usage >&2; exit 1 ;;
    esac
done

case "$RID" in
    linux-x64) ARCH=x86_64 ;;
    linux-arm64) ARCH=aarch64 ;;
    *) echo "unknown RID '$RID'" >&2; exit 1 ;;
esac
if [ "$RID" != "$HOST_RID" ]; then
    echo "NativeAOT cannot build $RID on $HOST_RID" >&2
    exit 1
fi

PROJECT="$ROOT/samples/AvaAdwaita.Gallery.Desktop"
WORK="$ROOT/artifacts/appimage-work/$RID"
APPDIR="$WORK/AvaAdwaita-Gallery.AppDir"
rm -rf "$WORK"
mkdir -p "$APPDIR/usr/bin" "$OUT"

VERSION="$(dotnet msbuild "$PROJECT" -getProperty:AvaAdwaitaLibadwaitaVersion)"
if [ "$REVISION" != 0 ]; then
    VERSION="$VERSION.$REVISION"
fi

dotnet publish "$PROJECT" -c Release -r "$RID" -p:PublishAot=true -p:AvaAdwaitaRevision="$REVISION" -o "$WORK/publish"

cp -a "$WORK"/publish/. "$APPDIR/usr/bin/"
rm -f "$APPDIR"/usr/bin/*.dbg "$APPDIR"/usr/bin/*.pdb "$APPDIR"/usr/bin/*.xml

cp "$PROJECT/AppImage/AppRun" "$APPDIR/AppRun"
chmod +x "$APPDIR/AppRun"
cp "$PROJECT/AppImage/avaadwaita-gallery.desktop" "$PROJECT/AppImage/avaadwaita-gallery.svg" "$APPDIR/"
ln -s avaadwaita-gallery.svg "$APPDIR/.DirIcon"

TOOL="$(command -v appimagetool || true)"
if [ -z "$TOOL" ]; then
    TOOL="$ROOT/artifacts/tools/appimagetool-$APPIMAGETOOL_VERSION-$(uname -m).AppImage"
    if [ ! -x "$TOOL" ]; then
        mkdir -p "$(dirname "$TOOL")"
        curl -fsSL -o "$TOOL" \
            "https://github.com/AppImage/appimagetool/releases/download/$APPIMAGETOOL_VERSION/appimagetool-$(uname -m).AppImage"
        chmod +x "$TOOL"
    fi
fi

TARGET="$OUT/AvaAdwaita-Gallery-$VERSION-$ARCH.AppImage"
APPIMAGE_EXTRACT_AND_RUN=1 ARCH=$ARCH VERSION=$VERSION "$TOOL" --no-appstream "$APPDIR" "$TARGET"
echo "$TARGET"
