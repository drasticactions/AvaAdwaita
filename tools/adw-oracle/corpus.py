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
                if w["type"] in ["clamp", "avatar"]:
                    parts.append(str(w.get("width", w.get("size"))))
                if w.get("initials"):
                    parts.append("initials")
                if "mode" in w:
                    parts.append(w["mode"])
                if "style" in w:
                    parts.append(w["style"])
                if w.get("bottom"):
                    parts.append("bottom")
                if "discrete" in w:
                    parts.append("discrete")
                if w.get("centered"):
                    parts.append("centered")
                if "pages" in w:
                    parts.append(f"{w['pages']}pages")
                if "selected" in w:
                    parts.append(f"sel{w['selected']}")
                if "rows" in w:
                    parts.append(f"{len(w['rows'])}rows")
                if "title" in w:
                    parts.append("longtitle")
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
                for flag in ["subtitle", "description", "prefix", "suffix", "activatable", "apply", "switch", "separate", "start-icon", "end-icon", "open", "button", "suggested", "nobuttons", "wide", "badge", "attention", "collapsed", "hidden", "end", "sheet", "destructive", "bar", "narrow", "nohandle", "search", "comments", "pinned", "inverted", "fixed", "page-icon"]:
                    if w.get(flag):
                        parts.append(flag)
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
# libadwaita's preference rows, each in a boxed list, with the states on the row.
ROW = {"width": 360}
add({"type": "actionrow", **ROW}, [[], ["disabled"]])
add({"type": "actionrow", "subtitle": "Subtitle", **ROW}, [[], ["disabled"]])
add({"type": "actionrow", "subtitle": "Subtitle", "prefix": True, "suffix": True, **ROW}, [[], ["disabled"]])
add({"type": "actionrow", "title": "A title long enough that the row has to wrap it onto a second line", "suffix": True, **ROW}, [[]])
add({"type": "actionrow", "activatable": True, **ROW}, [[], ["hover"], ["active"], ["focus-visible"]], accent_sensitive=True)
for active in [False, True]:
    add({"type": "switchrow", "active": active, **ROW}, [[], ["hover"], ["disabled"]], accent_sensitive=active)
for classes in [[], ["suggested-action"], ["destructive-action"]]:
    add({"type": "buttonrow", "classes": classes, **ROW}, [[], ["hover"], ["active"], ["disabled"]], accent_sensitive=classes == ["suggested-action"])
add({"type": "buttonrow", "start-icon": "list-add-symbolic", **ROW}, [[]])
add({"type": "buttonrow", "end-icon": "go-next-symbolic", **ROW}, [[]])
add({"type": "spinrow", "value": 40, **ROW}, [[], ["hover"], ["disabled"]], accent_sensitive=True)
add({"type": "spinrow", "value": 40, "title": "A much longer title for the row", **ROW}, [[]])
add({"type": "comborow", **ROW}, [[], ["hover"], ["active"], ["disabled"]])
add({"type": "comborow", "open": True, **ROW}, [[]], accent_sensitive=True)
for active in [False, True]:
    add({"type": "expanderrow", "active": active, **ROW}, [[], ["hover"], ["disabled"]], accent_sensitive=active)
add({"type": "expanderrow", "active": True, "subtitle": "Subtitle", "switch": True, **ROW}, [[]], accent_sensitive=True)
add({"type": "expanderrow", "prefix": True, "suffix": True, **ROW}, [[]])
for text in [{}, {"text": "Entry text"}]:
    add({"type": "entryrow", **text, **ROW}, [[], ["hover"], ["disabled"]])
add({"type": "entryrow", "text": "Entry text", "prefix": True, "suffix": True, **ROW}, [[]])
for text in [{}, {"text": "secret"}]:
    add({"type": "passwordentryrow", **text, **ROW}, [[], ["disabled"]])
# AdwPreferencesGroup with two action rows.
add({"type": "preferencesgroup", **ROW}, [[], ["backdrop"]])
add({"type": "preferencesgroup", "description": "A description of the group that is long enough to wrap", **ROW}, [[]])
add({"type": "preferencesgroup", "suffix": True, **ROW}, [[]])
add({"type": "preferencesgroup", "description": "Description", "suffix": True, "separate": True, **ROW}, [[]])
# AdwClamp around a button, below and in its tightening range; above it the scene would not fit the test window.
for width in [360, 450, 520]:
    add({"type": "clamp", "width": width}, [[]])
add({"type": "preferencespage", "width": 500}, [[]])
add({"type": "preferencespage", "width": 500, "description": "A description of the page", "centered": True}, [[], ["backdrop"]])
# GtkLevelBar: its default offsets are low, high and full at 0.25, 0.75 and 1.
for value in [0.2, 0.5, 1]:
    add({"type": "levelbar", "value": value, "width": 200}, [[], ["backdrop"]], accent_sensitive=True)
add({"type": "levelbar", "value": 40, "max": 100, "width": 200}, [[]])
add({"type": "levelbar", "value": 3, "max": 5, "discrete": 5, "width": 200}, [[], ["disabled"]], accent_sensitive=True)
# AdwAvatar: initials, sized to the circle, and the icon; a name picks the color.
for size in [24, 48, 96]:
    add({"type": "avatar", "size": size, "text": "Ada Lovelace", "initials": True}, [[]])
for name, key in [("Grace Hopper", "grace"), ("Linus", "linus"), ("Émile Zola", "emile")]:
    add({"type": "avatar", "size": 48, "text": name, "initials": True, "name": key}, [[]])
add({"type": "avatar", "size": 64, "text": "Ada Lovelace"}, [[]])
# AdwStatusPage: icon, title, description and a child button.
add({"type": "statuspage", "width": 400}, [[]])
# The full-size page with a button is taller than the test window; the compact one shows the button.
add({"type": "statuspage", "width": 400, "icon": "system-search-symbolic", "description": "Try a different search"}, [[], ["backdrop"]])
add({"type": "statuspage", "width": 400, "icon": "system-search-symbolic", "description": "Try a different search", "suffix": True, "classes": ["compact"]}, [[]])
# AdwBanner: the title alone, beside a button, and over a button when the two do not fit side by side.
add({"type": "banner", "width": 400}, [[], ["backdrop"]])
add({"type": "banner", "width": 400, "button": "Undo"}, [[], ["backdrop"]])
add({"type": "banner", "width": 400, "button": "Undo", "suggested": True}, [[]], accent_sensitive=True)
add({"type": "banner", "width": 240, "title": "A message long enough to wrap", "button": "Show Details"}, [[]])
add({"type": "windowtitle"}, [[], ["backdrop"]])
add({"type": "windowtitle", "subtitle": "Subtitle"}, [[], ["backdrop"]])
# AdwHeaderBar as a widget of its own: the window's title or a title widget, packed buttons, and window buttons.
add({"type": "adwheaderbar", "width": 400}, [[], ["backdrop"]])
add({"type": "adwheaderbar", "width": 400, "title": "Title", "subtitle": "Subtitle", "prefix": True, "suffix": True}, [[], ["backdrop"]])
add({"type": "adwheaderbar", "width": 400, "nobuttons": True, "prefix": True}, [[]])
# GtkActionBar: start, end (packed from the end) and center widgets.
add({"type": "actionbar", "width": 400}, [[], ["backdrop"]])
add({"type": "actionbar", "width": 400, "suffix": True, "title": "3 items selected"}, [[], ["backdrop"]])
# AdwToolbarView with a header bar (no window buttons) and an action bar, in each bar style.
add({"type": "toolbarview", "width": 400}, [[], ["backdrop"]])
for style in ["flat", "raised", "raised-border"]:
    add({"type": "toolbarview", "width": 400, "bottom": True, "style": style}, [[], ["backdrop"]])
# AdwViewStack of three pages and AdwViewSwitcher for it: wide with a badge and an attention mark, and narrow.
add({"type": "viewstack"}, [[]])
add({"type": "viewswitcher", "wide": True}, [[], ["backdrop"]])
add({"type": "viewswitcher", "wide": True, "badge": True, "attention": True}, [[]], accent_sensitive=True)
add({"type": "viewswitcher"}, [[], ["backdrop"]])
add({"type": "viewswitcher", "badge": True, "attention": True}, [[]], accent_sensitive=True)
add({"type": "viewswitcherbar", "width": 400}, [[], ["backdrop"]])
# AdwInlineViewSwitcher in each display mode.
for mode in ["labels", "icons", "both"]:
    add({"type": "inlineviewswitcher", "mode": mode}, [[], ["backdrop"]], accent_sensitive=True)
for mode in ["labels", "icons", "both"]:
    add({"type": "inlineviewswitcher", "mode": mode, "badge": True, "attention": True}, [[]], accent_sensitive=True)
add({"type": "stackswitcher"}, [[], ["backdrop"]])
# AdwOverlaySplitView: expanded with the sidebar at either end, and collapsed with the sidebar over the content or hidden.
SPLIT = {"width": 500, "height": 200}
add({"type": "overlaysplitview", **SPLIT}, [[], ["backdrop"]])
add({"type": "overlaysplitview", "end": True, **SPLIT}, [[]])
add({"type": "overlaysplitview", "collapsed": True, **SPLIT}, [[], ["backdrop"]])
add({"type": "overlaysplitview", "collapsed": True, "hidden": True, **SPLIT}, [[]])
# AdwNavigationView at its root and with a page pushed (a back button shows), and AdwNavigationSplitView expanded and
# collapsed, at the sidebar and at the content. Each page is a toolbar view with a header bar.
add({"type": "navigationview", "width": 400, "height": 200}, [[], ["backdrop"]])
add({"type": "navigationview", "width": 400, "height": 200, "active": True}, [[], ["backdrop"]])
add({"type": "navigationsplitview", **SPLIT}, [[], ["backdrop"]])
add({"type": "navigationsplitview", "collapsed": True, **SPLIT}, [[]])
add({"type": "navigationsplitview", "collapsed": True, "active": True, **SPLIT}, [[]])
# GtkSearchEntry: empty with a placeholder at its natural width (which keeps room for the clear icon), and with text
# and the clear icon.
add({"type": "searchentry", "placeholder": "Search"}, [[], ["focus-visible"], ["backdrop"]])
add({"type": "searchentry", "width": 240, "text": "Query"}, [[], ["focus-visible"]])
# GtkSearchBar with a search entry, searching, with and without its close button.
add({"type": "searchbar", "width": 400}, [[], ["backdrop"]])
add({"type": "searchbar", "width": 400, "button": True}, [[]])
# AdwTabBar over an AdwTabView: three tabs, the first selected; one tab; tabs that do not expand; a pinned tab; the
# close buttons first; an end action; and a tab that needs attention.
TABS = {"width": 500}
add({"type": "tabbar", **TABS}, [[], ["backdrop"]])
add({"type": "tabbar", "selected": 1, "page-icon": True, **TABS}, [[]])
add({"type": "tabbar", "pages": 1, **TABS}, [[], ["backdrop"]])
add({"type": "tabbar", "fixed": True, **TABS}, [[]])
add({"type": "tabbar", "pinned": True, **TABS}, [[], ["backdrop"]])
add({"type": "tabbar", "inverted": True, **TABS}, [[]])
add({"type": "tabbar", "button": True, **TABS}, [[]])
add({"type": "tabbar", "attention": True, "selected": 2, **TABS}, [[]], accent_sensitive=True)
add({"type": "tabview", "width": 300, "height": 100}, [[]])
# AdwTabButton counting the pages: three, twelve (a smaller count) and with a page other than the selected one needing
# attention.
add({"type": "tabbutton"}, [[], ["hover"], ["backdrop"]])
add({"type": "tabbutton", "pages": 12}, [[]])
add({"type": "tabbutton", "attention": True}, [[]], accent_sensitive=True)
# AdwTabOverview open over its tab view: its pages, with the new tab button, and with a pinned page.
add({"type": "taboverview", "width": 540, "height": 340, "active": True}, [[]])
add({"type": "taboverview", "width": 540, "height": 340, "active": True, "button": True, "attention": True}, [[]])
add({"type": "taboverview", "width": 540, "height": 340, "active": True, "pinned": True, "attention": True}, [[]])
# AdwShortcutLabel: a shortcut, alternatives, a range, keys at once, a sequence, and none.
for name, accel in [("single", "<Control>C"), ("alternatives", "<Shift>A Home"), ("range", "<Alt>1...9"),
                    ("together", "Control_L&Control_R"), ("sequence", "<Control>C+<Control>X"),
                    ("modifiers", "<Control><Shift>Page_Up"), ("none", "")]:
    add({"type": "shortcutlabel", "label": accel, "name": name}, [[]])
# AdwWrapBox of seven buttons wrapping at a natural line length of 240 px: from the start, centered, filling and spread.
add({"type": "wrapbox", "length": 240, "height": 114}, [[]])
add({"type": "wrapbox", "length": 240, "height": 114, "centered": True}, [[]])
add({"type": "wrapbox", "length": 240, "height": 114, "mode": "fill"}, [[]])
add({"type": "wrapbox", "length": 240, "height": 114, "mode": "spread"}, [[]])
# AdwBottomSheet over a label: open with a toolbar view and header bar in the sheet, with and without the drag handle,
# as wide as the sheet's content, and with a bottom bar while closed and open.
SHEET = {"width": 400, "height": 300}
add({"type": "bottomsheet", "active": True, **SHEET}, [[], ["backdrop"]])
add({"type": "bottomsheet", "active": True, "nohandle": True, **SHEET}, [[]])
add({"type": "bottomsheet", "active": True, "narrow": True, **SHEET}, [[]])
add({"type": "bottomsheet", "bar": True, **SHEET}, [[], ["backdrop"]])
add({"type": "bottomsheet", "bar": True, "narrow": True, **SHEET}, [[]])
add({"type": "bottomsheet", "bar": True, "active": True, **SHEET}, [[]])
# AdwDialog presented in the window, floating: a header bar over a label, 300 by 200. The scene is its sheet. The oracle
# sets a scene's states on the control the dialog is presented from, so dialog scenes have none.
add({"type": "dialog"}, [[]])
add({"type": "dialog", "sheet": True}, [[]])
# AdwAlertDialog: two responses side by side, and three, which stack (the destructive one widens the dialog past 372).
add({"type": "alertdialog"}, [[]])
add({"type": "alertdialog", "destructive": True}, [[]])
add({"type": "alertdialog", "sheet": True}, [[]])
# AdwShortcutsDialog with a section of two shortcuts, one with a subtitle, and a section of one, its height set once
# it shows, as libadwaita resets it then.
add({"type": "shortcutsdialog", "content-width": 420, "content-height": 300}, [[]])
# AdwAboutDialog's main page: icon, name, developer and version over the website, issue, credits and legal rows; with
# comments, the website moves into the details page and a details row shows.
add({"type": "aboutdialog", "content-height": 360}, [[]])
add({"type": "aboutdialog", "content-height": 360, "comments": True}, [[]])
# AdwPreferencesDialog with two pages: narrow, its title in the header bar and the view switcher bar at the bottom, and
# wide enough for the view switcher in the header bar, with the search button.
add({"type": "preferencesdialog", "content-width": 360, "content-height": 300}, [[]])
add({"type": "preferencesdialog", "content-width": 500, "content-height": 300, "search": True, "wide": True}, [[]])
add({"type": "button", "label": "Button"}, [[]], rtl=True)
add({"type": "checkbutton", "label": "Check", "active": True}, [[]], rtl=True)
add({"type": "switch", "active": True}, [[]], rtl=True)
add({"type": "actionrow", "subtitle": "Subtitle", "prefix": True, "suffix": True, **ROW}, [[]], rtl=True)
add({"type": "entryrow", "text": "Entry text", **ROW}, [[]], rtl=True)

ids = set()
for e in entries:
    assert e["id"] not in ids, e["id"]
    ids.add(e["id"])

json.dump(entries, sys.stdout, indent=1)
sys.stdout.write("\n")
print(len(entries), "entries", file=sys.stderr)
