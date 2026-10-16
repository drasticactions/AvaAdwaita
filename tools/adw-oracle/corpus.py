#!/usr/bin/env python3
"""Writes the parity corpus: widget scenes x states x style classes x settings, as JSON."""
import json
import sys

SCHEMES = [("light", False, False), ("dark", True, False), ("hc", False, True), ("hcdark", True, True)]
ACCENTS = ["blue", "yellow", "slate"]
COMMON = [[], ["hover"], ["active"], ["disabled"], ["focus-visible"], ["backdrop"]]

entries = []


def add(widget, states_list, accent_sensitive=False, rtl=False):
    for states in states_list:
        for scheme, dark, hc in SCHEMES:
            for accent in ACCENTS if accent_sensitive else ["blue"]:
                w = dict(widget)
                if states:
                    w["states"] = states
                parts = [w["type"]]
                if "icon" in w:
                    parts.append("icon")
                if w.get("active"):
                    parts.append("on")
                if w.get("inconsistent"):
                    parts.append("mixed")
                if "value" in w:
                    parts.append(str(w["value"]))
                if w.get("visited"):
                    parts.append("visited")
                if "position" in w:
                    parts.append(w["position"])
                if "rows" in w:
                    parts.append(f"{len(w['rows'])}rows")
                if "text" in w:
                    parts.append("text")
                if w.get("row-separators"):
                    parts.append("rowlines")
                if w.get("column-separators"):
                    parts.append("collines")
                if w.get("shapes"):
                    parts.append("shapes")
                if w.get("classic"):
                    parts.append("classic")
                if "name" in w:
                    parts.append(w["name"])
                if "marks" in w:
                    parts.append("marks" + w["marks"])
                parts += w.get("classes", [])
                parts += states
                if rtl:
                    parts.append("rtl")
                parts += [scheme, accent]
                e = {"id": "-".join(parts), "widget": w, "dark": dark, "hc": hc, "accent": accent}
                if rtl:
                    e["rtl"] = True
                entries.append(e)


for classes in [[], ["suggested-action"], ["destructive-action"], ["flat"], ["pill"], ["opaque"], ["osd"]]:
    add({"type": "button", "label": "Button", "classes": classes}, COMMON, accent_sensitive=classes == ["suggested-action"])
for classes in [[], ["circular"], ["flat"], ["osd", "circular"]]:
    add({"type": "button", "icon": "list-add-symbolic", "classes": classes}, [[], ["hover"], ["active"], ["disabled"]])
for active in [False, True]:
    for classes in [[], ["flat"]]:
        add({"type": "togglebutton", "label": "Toggle", "active": active, "classes": classes}, [[], ["hover"], ["active"], ["disabled"]])
for active, mixed in [(False, False), (True, False), (False, True)]:
    w = {"type": "checkbutton", "label": "Check", "active": active}
    if mixed:
        w["inconsistent"] = True
    add(w, COMMON, accent_sensitive=active or mixed)
add({"type": "checkbutton", "label": "Check", "active": True, "classes": ["selection-mode"]}, [[]], accent_sensitive=True)
for active in [False, True]:
    add({"type": "radiobutton", "label": "Radio", "active": active}, COMMON, accent_sensitive=active)
    add({"type": "switch", "active": active}, [[], ["hover"], ["active"], ["disabled"], ["focus-visible"]], accent_sensitive=active)
for text in [{"placeholder": "Placeholder"}, {"text": "Entry text"}]:
    add(dict({"type": "entry", "width": 200}, **text), [[], ["focus-visible"], ["disabled"], ["backdrop"]], accent_sensitive="text" in text)
for classes in [["error"], ["warning"], ["success"], ["flat"]]:
    add({"type": "entry", "width": 200, "text": "Entry text", "classes": classes}, [[], ["focus-visible"]])
for classes in [[], ["title-1"], ["title-2"], ["title-3"], ["title-4"], ["heading"], ["body"], ["caption-heading"], ["caption"],
                ["monospace"], ["numeric"], ["dim-label"], ["accent"], ["success"], ["warning"], ["error"]]:
    add({"type": "label", "label": "The quick brown fox", "classes": classes}, [[], ["disabled"]], accent_sensitive=classes == ["accent"])
for n in [1, 2, 3]:
    rows = ["Row one", "Row two", "Row three"][:n]
    for classes in [["boxed-list"], ["boxed-list-separate"], ["navigation-sidebar"], ["rich-list"]]:
        add({"type": "listbox", "rows": rows, "width": 240, "classes": classes}, [[]])
for value in [0, 40, 100]:
    add({"type": "scale", "value": value, "width": 200}, [[], ["hover"], ["disabled"]], accent_sensitive=value == 40)
    add({"type": "progressbar", "value": value, "width": 200}, [[]], accent_sensitive=value == 40)
add({"type": "progressbar", "value": 40, "width": 200, "classes": ["osd"]}, [[]])
add({"type": "separator", "width": 200}, [[]])
add({"type": "dropdown"}, [[], ["hover"], ["disabled"]])
for classes in [[], ["flat"]]:
    add({"type": "menubutton", "label": "Menu", "classes": classes}, [[], ["hover"], ["active"], ["disabled"], ["focus-visible"]])
for classes in [[], ["suggested-action"]]:
    add({"type": "splitbutton", "label": "Split", "classes": classes}, [[], ["disabled"]], accent_sensitive=classes == ["suggested-action"])
add({"type": "spinbutton", "value": 40}, [[], ["focus-visible"], ["disabled"]], accent_sensitive=True)
for visited in [False, True]:
    add({"type": "linkbutton", "label": "Link", "visited": visited}, [[], ["hover"]], accent_sensitive=not visited)
for position in ["top", "bottom"]:
    add({"type": "notebook", "position": position}, [[], ["backdrop"]])
for active in [False, True]:
    add({"type": "expander", "label": "Expander", "active": active}, [[], ["hover"], ["disabled"]])
add({"type": "frame", "label": "Frame"}, [[]])
add({"type": "splitview"}, [[]])
add({"type": "togglegroup"}, [[]], accent_sensitive=True)
for active in [False, True]:
    add({"type": "treeview", "active": active}, [[]])
add({"type": "columnview"}, [[]])
# AvaAdwaita.DataGrid's DataGrid against the same column view.
for extra in [{}, {"active": True}, {"row-separators": True}, {"column-separators": True}, {"classes": ["data-table"]}]:
    add({"type": "datagrid", **extra}, [[]] if extra else [[], ["backdrop"]], accent_sensitive=not extra)
for marks in ["bottom", "top"]:
    add({"type": "scale", "value": 40, "width": 200, "marks": marks}, [[], ["hover"], ["disabled"]], accent_sensitive=True)
add({"type": "menubar"}, [[], ["backdrop"]])
add({"type": "paned"}, [[], ["backdrop"]])
add({"type": "spinner"}, [[], ["backdrop"]])
add({"type": "tooltip", "label": "Tip"}, [[]])
add({"type": "carouseldots"}, [[], ["backdrop"]], accent_sensitive=True)
add({"type": "toast", "label": "Saved"}, [[], ["backdrop"]])
add({"type": "vspinbutton", "value": 30}, [[], ["disabled"], ["backdrop"]], accent_sensitive=True)
add({"type": "calendar"}, [[], ["backdrop"], ["disabled"]], accent_sensitive=True)
add({"type": "popovermenu"}, [[]], accent_sensitive=True)
# The window's own header bar: title and window buttons.
add({"type": "headerbar"}, [[], ["backdrop"]])
for classic in [False, True]:
    w = {"type": "scrolledwindow"}
    if classic:
        w["classic"] = True
    add(w, [[], ["backdrop"]])
add({"type": "textview", "text": "Text"}, [[], ["focus-visible"], ["disabled"], ["backdrop"]], accent_sensitive=True)
# The on and off marks show with the status shapes setting.
for active in [False, True]:
    add({"type": "switch", "active": active, "shapes": True}, [[], ["disabled"]], accent_sensitive=active)
# The color filters, from an application style sheet.
for name, f in [("grayscale", "grayscale(100%)"), ("sepia", "sepia(60%) hue-rotate(90deg)"), ("invert", "invert(100%)"),
                ("tones", "brightness(80%) contrast(150%) saturate(200%)")]:
    add({"type": "button", "label": "Button", "classes": ["suggested-action", "filtered"], "name": name,
         "css": ".filtered { filter: " + f + "; }"}, [[], ["hover"]])
add({"type": "button", "label": "Button"}, [[]], rtl=True)
add({"type": "checkbutton", "label": "Check", "active": True}, [[]], rtl=True)
add({"type": "switch", "active": True}, [[]], rtl=True)

ids = set()
for e in entries:
    assert e["id"] not in ids, e["id"]
    ids.add(e["id"])

json.dump(entries, sys.stdout, indent=1)
sys.stdout.write("\n")
print(len(entries), "entries", file=sys.stderr)
