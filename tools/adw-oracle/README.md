# adw-oracle

Builds widgets with the real GTK 4.24 and libadwaita 1.10 and writes what they are and what
they draw, as the reference for AvaAdwaita's parity tests.

```sh
make OUT=../../artifacts/adw-oracle          # needs gtk4, libadwaita-1 and json-glib-1.0
```

Under a Wayland compositor (CI and `tools/regenerate-data.sh` use `weston --backend=headless`):

```sh
adw-oracle --tree TYPE                       # the widget tree of one widget type
adw-oracle --corpus corpus.json --out expected-gtk.json [--goldens DIR]
```

- `corpus.py` writes the corpus: each scene is a widget type, its style classes, its
  states, and the settings (dark, high contrast, accent).
- For each scene the oracle presents the widget in an `AdwWindow` with an
  `AdwToolbarView`, sets the states with `gtk_widget_set_state_flags`, waits for frames,
  and records the widget tree with allocations, the render nodes reduced to draws
  (colors converted from their color state to sRGB), and a PNG of the widget with a
  12 px margin.
- libadwaita reads `ADW_DEBUG_HIGH_CONTRAST` and `ADW_DEBUG_ACCENT_COLOR` only at
  start-up, so the program runs itself once per contrast and accent and merges the results.
- Weston runs with the pixman renderer: the headless backend without one crashes when GTK
  opens a popup. No widget keeps keyboard focus, and the caret does not blink, so the data
  does not depend on timing.
- Run it with `GSETTINGS_BACKEND=memory`, `GDK_DEBUG=no-portals` and an empty `XDG_CONFIG_HOME`
  so the host's fonts, icon theme and other settings do not leak in through GSettings, the
  settings portal or `settings.ini`; `tools/regenerate-data.sh` does.

The supported types are those in `build()`: button, togglebutton, checkbutton,
radiobutton, switch, entry, label, listbox, scale, progressbar, separator, dropdown,
menubutton, splitbutton, spinbutton, vspinbutton, linkbutton, notebook, togglegroup,
expander, frame, paned, splitview, treeview, columnview and calendar.
