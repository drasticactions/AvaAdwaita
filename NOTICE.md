# Third-party notices

AvaAdwaita is licensed under the GNU Lesser General Public License, version 2.1
or (at your option) any later version (`LGPL-2.1-or-later`). See `LICENSE`.

## libadwaita 1.10.0

<https://gitlab.gnome.org/GNOME/libadwaita> — `LGPL-2.1-or-later`.
Copyright © the libadwaita authors.

**Embedded:** the compiled stylesheet `gtk.css`
(`src/AvaAdwaita.Theme/Sources/Bundled/gtk-1.10.0.css`, built from
`src/stylesheet` with `sassc -a -M -t compact`) and its four assets
(`bullet.svg`, `check.svg`, `dash.svg`, `devel-stripes.svg`). At run time
AvaAdwaita reads the same stylesheet from the installed `libadwaita-1.so`
when it is present and parses cleanly.

**Ported to C#:**

- accent color snapping and the standalone accent colors
  (`adw-accent-color.c`, `adw_rgba_to_standalone`);
- the accent and fonts style providers (`adw-style-manager.c`);
- the easing curves (`adw-easing.c`) and the spring solver
  (`adw-spring-animation.c`);
- the settings keys and their meaning (`adw-settings-impl-portal.c`,
  `adw-settings-impl-gsettings.c`).

**Icons:** the 19 `adw-*-symbolic.svg` icons from `src/icons` are `CC0-1.0`.

## GTK 4.24.1

<https://gitlab.gnome.org/GNOME/gtk> — `LGPL-2.1-or-later`.
Copyright © the GTK authors.

**Ported to C#:** the CSS tokenizer and parser semantics, selector matching
and specificity, the cascade and computed values (`gtk/gtkcss*.c`),
`color-mix()`, relative colors and OKLab/OKLCH conversion
(`gtkcsscolor.c`, `gtkcsscolorvalue.c`), box-shadow extents and blur
(`gtkcssshadowvalue.c`, `gsk/gskroundedrect.c`), transition interpolation
(`gtkcsstransition.c`, `gtkcssanimatedstyle.c`) and the CSS node trees of
GTK's widgets. Color conformance cases are ported from `testsuite/css`.

**Icons:** `switch-on-symbolic.svg` and `switch-off-symbolic.svg` come from
GTK's built-in icons (`gtk/icons`), `LGPL-2.1-or-later`.

## adwaita-icon-theme 51.0

<https://gitlab.gnome.org/GNOME/adwaita-icon-theme> — used under the Creative
Commons Attribution-ShareAlike 3.0 Unported license (`CC-BY-SA-3.0`), one of
the two licenses the theme is offered under. Copyright © the GNOME Project.
The icons are unmodified and kept as data files in
`src/AvaAdwaita.Theme/Icons/Bundled/` with `LICENSE.icons`. They are used on
platforms without a freedesktop icon theme, and as a fallback.

Bundled icons: `alarm-symbolic`, `audio-volume-high-symbolic`, `dialog-error-symbolic`, `dialog-information-symbolic`, `dialog-warning-symbolic`, `document-open-symbolic`, `document-save-symbolic`, `edit-clear-symbolic`, `edit-copy-symbolic`, `edit-cut-symbolic`, `edit-find-symbolic`, `edit-paste-symbolic`, `folder-symbolic`, `go-down-symbolic`, `go-next-symbolic-rtl`, `go-next-symbolic`, `go-previous-symbolic-rtl`, `go-previous-symbolic`, `go-up-symbolic`, `help-about-symbolic`, `list-add-symbolic`, `list-remove-symbolic`, `mail-unread-symbolic`, `media-playback-start-symbolic`, `object-select-symbolic`, `open-menu-symbolic`, `pan-down-symbolic`, `pan-end-symbolic-rtl`, `pan-end-symbolic`, `pan-start-symbolic-rtl`, `pan-start-symbolic`, `pan-up-symbolic`, `preferences-system-symbolic`, `process-stop-symbolic`, `sidebar-show-symbolic`, `star-new-symbolic`, `system-search-symbolic`, `user-trash-symbolic`, `value-decrease-symbolic`, `value-increase-symbolic`, `view-conceal-symbolic`, `view-grid-symbolic`, `view-list-symbolic`, `view-more-horizontal-symbolic`, `view-more-symbolic`, `view-refresh-symbolic`, `view-reveal-symbolic`, `window-close-symbolic`, `window-maximize-symbolic`, `window-minimize-symbolic`, `window-restore-symbolic`, `x-office-calendar-symbolic`, `text-x-generic-symbolic`, `drive-harddisk-symbolic`.

## Adwaita fonts 51.0

<https://gitlab.gnome.org/GNOME/adwaita-fonts> — Adwaita Sans and Adwaita Mono,
SIL Open Font License 1.1 (`OFL-1.1`), shipped in `AvaAdwaita.Fonts` with
`OFL.txt`. Adwaita Mono is unmodified. Adwaita Sans is distributed upstream as
variable fonts; The license declares no
Reserved Font Name, and the instances keep the family name. Adwaita Sans is
derived from Inter; Adwaita Mono from Iosevka.

## Noto Sans CJK 2.004

<https://github.com/notofonts/noto-cjk> — `NotoSansCJKjp-Regular.otf`, SIL Open
Font License 1.1 (`OFL-1.1`), unmodified, with `OFL.txt`.

Used only in the Browser sample.

## Avalonia

<https://github.com/AvaloniaUI/Avalonia> — `MIT`. Copyright © .NET Foundation
and Contributors.

## Tmds.DBus.Protocol

<https://github.com/tmds/Tmds.DBus> — `MIT`. Used to read the settings portal.
