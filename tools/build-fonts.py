#!/usr/bin/env python3
"""Writes src/AvaAdwaita.Fonts/Assets from the Adwaita fonts.

Adwaita Sans ships as variable fonts. Avalonia selects faces by the OS/2 weight of whole font files and
cannot set a variable font's axes, so bold text would come out regular. This writes a static instance per
weight (opsz at its default, 14) with fontTools; the names are the instances' own (the OFL declares no
Reserved Font Name). Adwaita Mono ships static files, which are copied unchanged.

    tools/build-fonts.py [SOURCE_DIR]     default /usr/share/fonts/Adwaita
"""
import pathlib
import shutil
import sys

from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

source = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "/usr/share/fonts/Adwaita")
out = pathlib.Path(__file__).resolve().parent.parent / "src" / "AvaAdwaita.Fonts" / "Assets"
out.mkdir(parents=True, exist_ok=True)
for old in out.glob("*.ttf"):
    old.unlink()

WEIGHTS = {300: "Light", 400: "Regular", 500: "Medium", 600: "SemiBold", 700: "Bold", 800: "ExtraBold", 900: "Black"}

def set_names(font, weight_name, italic):
    """Names the instance as the variable font's named instance: family Adwaita Sans, style by weight."""
    style = weight_name if not italic else ("Italic" if weight_name == "Regular" else f"{weight_name} Italic")
    ribbi = weight_name in ("Regular", "Bold")
    legacy_family = "Adwaita Sans" if ribbi else f"Adwaita Sans {weight_name}"
    legacy_style = ("Bold" if weight_name == "Bold" else "Regular") if not italic else ("Bold Italic" if weight_name == "Bold" else "Italic")
    name = font["name"]
    for rec in list(name.names):
        if rec.nameID in (1, 2, 3, 4, 6, 16, 17, 21, 22, 25):
            name.removeNames(nameID=rec.nameID)
    name.setName(legacy_family, 1, 3, 1, 0x409)
    name.setName(legacy_style, 2, 3, 1, 0x409)
    name.setName(f"Adwaita Sans {style}".replace(" Regular", ""), 4, 3, 1, 0x409)
    ps = "AdwaitaSans-" + style.replace(" ", "")
    name.setName(f"{font['head'].fontRevision:.3f};{ps}", 3, 3, 1, 0x409)
    name.setName(ps, 6, 3, 1, 0x409)
    name.setName("Adwaita Sans", 16, 3, 1, 0x409)
    name.setName(style, 17, 3, 1, 0x409)
    os2 = font["OS/2"]
    os2.fsSelection &= ~(1 | 32 | 64)
    if italic:
        os2.fsSelection |= 1
    if weight_name == "Bold":
        os2.fsSelection |= 32
    if not italic and weight_name == "Regular":
        os2.fsSelection |= 64
    font["head"].macStyle = (1 if weight_name == "Bold" else 0) | (2 if italic else 0)


for style, suffix in (("Regular", ""), ("Italic", "Italic")):
    for weight, name in WEIGHTS.items():
        font = TTFont(source / f"AdwaitaSans-{style}.ttf")
        static = instancer.instantiateVariableFont(font, {"wght": weight, "opsz": None}, updateFontNames=True)
        # opsz stays variable above only if requested; pin it to its default.
        if "fvar" in static:
            static = instancer.instantiateVariableFont(static, {a.axisTag: a.defaultValue for a in static["fvar"].axes}, updateFontNames=True)
        static["OS/2"].usWeightClass = weight
        label = name if not suffix else (suffix if name == "Regular" else name + suffix)
        set_names(static, name, bool(suffix))
        static.save(out / f"AdwaitaSans-{label}.ttf")
        print(f"AdwaitaSans-{label}.ttf")

for f in sorted(source.glob("AdwaitaMono-*.ttf")):
    shutil.copy(f, out / f.name)
    print(f.name)
