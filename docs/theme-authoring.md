# Writing ControlThemes

A ControlTheme in AvaAdwaita uses libadwaita's `gtk.css` to style it. 
If the tree is right, the control looks right in light, dark and high contrast, with every accent, and with
any future libadwaita that keeps the same nodes.

## 1. Find the node tree

Every GTK widget documents its nodes under "CSS nodes" in its API
reference. For example `GtkCheckButton`:

```
checkbutton[.text-button][.grouped]
├── check
╰── [label]
```

To see the tree a real widget has, with the classes GTK adds itself, run the
GTK Inspector (`GTK_DEBUG=interactive`) on a libadwaita app, or
`tools/adw-oracle --tree` (see the oracle's README).

## 2. Mark the nodes

Set `adw:Css.Node` on the elements that correspond to nodes. Elements
without it are invisible to CSS, so layout panels can be added freely.

```xml
<ControlTheme x:Key="{x:Type CheckBox}" TargetType="CheckBox">
  <Setter Property="adw:Css.Node" Value="checkbutton" />
  <Setter Property="Template">
    <ControlTemplate>
      <adw:AdwBox>
        <adw:AdwStack Orientation="Horizontal">
          <adw:AdwIcon Name="PART_Check" adw:Css.Node="check" VerticalAlignment="Center" />
          <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}">
            <ContentPresenter.DataTemplates>
              <adw:LabelTemplate />
            </ContentPresenter.DataTemplates>
          </ContentPresenter>
        </adw:AdwStack>
      </adw:AdwBox>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

- The control itself is the `checkbutton` node. Its `AdwBox` draws that
  node's box (background, border, shadow, padding, `min-*`).
- `AdwStack` lays out children with the node's `border-spacing`, as
  `GtkBox` does.
- `AdwIcon` with `adw:Css.Node="check"` is its own node and draws its
  `-gtk-icon-source` (here the check mark asset).
- `LabelTemplate` turns string content into an `AdwLabel` with a `label`
  node, so text gets the node's color and font.

## 3. Classes

- The control's Avalonia `Classes` are the node's CSS classes:
  `<Button Classes="suggested-action pill" />`.
- `adw:Css.Classes="toggle horizontal"` adds classes to template parts.
- Classes GTK widgets add themselves (`.text-button`, `.image-button`,
  `.horizontal`/`.vertical`) come from hint providers in
  `Nodes/ControlHints.cs`. Add one there when a new control needs them.

## 4. States

States come from the element's pseudo-classes by default:

| Avalonia | GTK |
|---|---|
| `:pointerover` | `:hover` |
| `:pressed` | `:active` |
| `:checked` | `:checked` |
| `:indeterminate` | `:indeterminate` |
| `:disabled` | `:disabled` |
| `:focus` / `:focus-visible` / `:focus-within` | same |
| `:selected` | `:selected` |
| inactive window | `:backdrop` |

A template part that is a node of its own but should share the control's
state (a `trough` inside a progress bar, the arrow button inside a combo
box) sets `adw:Css.StateSource="TemplatedParent"`. `Self` ignores the
templated parent; `None` gives the node no state.

Items in a list set `adw:Css.Position="Item"` so `:first-child` and
`:last-child` come from the item index, which stays right under
virtualization.

A control that disables children only to make them read-only (a data grid's
check boxes outside the current cell) sets `adw:Css.Sensitive` on an
ancestor: nodes below take `:disabled` from it instead of from their own
enabled state.

## 5. Reading computed values

When a stock Avalonia element must take a value from CSS, bind it with
`{adw:Css}`:

```xml
<TextBlock Foreground="{adw:Css Core.Color}"
           FontWeight="{adw:Css Font.Weight}"
           LineHeight="{adw:Css LineHeight}" />
<Border CornerRadius="{adw:Css Border.Radius, Node=PART_Box}" />
```

The path is the computed-style group and property (see `CssPaths.g.cs` or
`docs/css-support.md`). `Node=` reads another named element's node instead
of the nearest one. Values convert to Avalonia types (`Color`, `IBrush`,
`Thickness`, `CornerRadius`, `FontWeight`, `double`).

Values on the window node are also resources:
`{DynamicResource Adw.AccentBgColorBrush}`, `Adw.WindowRadius`,
`Adw.Named.accent_color`.

## 6. Parts drawn in code

A few GTK widgets draw parts in code rather than in CSS (the switch
slider's position, the spinner, the scale's marks). Those are small
presenters (`SwitchLayout`, `SwitchKnobCanvas`) that still take every
color and size from nodes.

## Node trees

These are the trees GTK 4.24 and libadwaita 1.10 build for the widgets each Avalonia control stands for, as
`tools/adw-oracle --tree TYPE` prints them (mapped widgets only; classes as GTK adds them). The ControlThemes
reproduce them, and `ParityTests` compares the trees, allocations and draws of about 1,300 scenes with GTK's.
Calendar months, the calendar's header and the pickers differ where Avalonia's controls do (see the notes in
their `.axaml` files).

### Button — GtkButton

```
button.text-button
  label
```

### ToggleButton — GtkToggleButton

```
button.text-button.toggle
  label
```

### CheckBox — GtkCheckButton

```
checkbutton
  check
```

### RadioButton — GtkCheckButton in a group

```
checkbutton.grouped
  radio
```

### ToggleSwitch — GtkSwitch

```
switch
  image
  image
  slider
```

### TextBox — GtkEntry

```
entry
  text
```

With `AcceptsReturn`, a GtkTextView in a framed GtkScrolledWindow. Its text is
drawn in Pango's 16-bit color (`CssConverters.PangoForeground`), as GtkTextView's
is:

```
scrolledwindow.frame
  textview.view
    text
  scrollbar.vertical.right, scrollbar.horizontal.bottom   (when they show)
```

### Label — GtkLabel

```
label
```

### ListBox, ListBoxItem — GtkListBox

```
list
  row.activatable
    label
```

### Slider — GtkScale

```
scale.horizontal
  trough
    highlight.top
    slider
```

With ticks (`TickPlacement` and `TickFrequency` or `Ticks`), as `gtk_scale_add_mark`:
`TopLeft` is `.marks-before` with `marks.top` before the trough, `BottomRight` is
`.marks-after` with `marks.bottom` after it, `Outside` both. With marks on one side
libadwaita turns the slider into a diamond.

```
scale.horizontal.marks-after
  trough
    highlight.top
    slider
  marks.bottom
    mark
      indicator
```

### ProgressBar — GtkProgressBar

```
progressbar.horizontal
  trough
    progress.left
```

### Separator — GtkSeparator

```
separator.horizontal
```

### ComboBox, ComboBoxItem — GtkDropDown

```
dropdown
  button.toggle
    box.horizontal
      stack
        row.activatable
          box.horizontal
            label
      arrow
```

### DropDownButton — GtkMenuButton

```
menubutton.popup
  button.arrow-button.text-button.toggle
    box.horizontal
      label
      arrow.down
```

### SplitButton, ToggleSplitButton — AdwSplitButton

```
splitbutton.text-button
  button
    label
  separator.vertical
  menubutton.popup
    button.toggle
      arrow.down
```

### NumericUpDown, ButtonSpinner — GtkSpinButton

```
spinbutton.horizontal
  text
  button.image-button.down
    image
  button.image-button.up
    image
```

### HyperlinkButton — GtkLinkButton

```
button.flat.text-button.link
  label
```

### TabControl, TabItem, TabStrip — GtkNotebook

```
notebook.frame
  header.top.horizontal
    tabs
      tab
        label
      tab
        label
  stack
    label
```

### TabControl.view-switcher — AdwToggleGroup

```
toggle-group
  toggle.text-button.toggle
    label
  separator.hidden.horizontal
  toggle.text-button.toggle
    label
  separator.horizontal
  toggle.text-button.toggle
    label
```

### Expander — GtkExpander

```
expander-widget
  box.vertical
    title.horizontal
      expander.horizontal
      label
```

### GroupBox, HeaderedContentControl — GtkFrame

```
frame
  label
  label
```

### GridSplitter (with its neighbors) — GtkPaned

The Grid holding the splitter is the `paned`, and the splitter is its `separator`:

```
paned.horizontal
  label
  separator
  label
```

### SplitView — AdwOverlaySplitView

```
overlay-split-view
  widget.content-pane
    label
  widget.sidebar-pane
    label
```

### TreeView, TreeViewItem — GtkListView + GtkTreeExpander

```
listview.view
  row.activatable
    treeexpander
      expander
      label
  row.activatable
    treeexpander
      indent
      label
```

### TableView and its rows, cells and headers — GtkColumnView

```
columnview.view
  header.activatable
    button
      box.horizontal
        label
        sort-indicator.unsorted
    button
      box.horizontal
        label
        sort-indicator.unsorted
  listview.view
    row.activatable
      cell
        label
```

### DataGrid and its rows, cells and headers — GtkColumnView (AvaAdwaita.DataGrid)

```
columnview.view[.column-separators][.data-table]
  header.activatable
    button
      box.horizontal
        label
        sort-indicator.unsorted|.ascending|.descending
  listview.view[.separators]
    row.activatable
      cell
        label
    row.activatable                          (a group's header)
      cell
        treeexpander
          expander
          label
```

Vertical grid lines are `.column-separators` and horizontal ones `.separators`.
The grid's filler column has no nodes; rows, cells and headers take their
positions from the grid's display order.

### Calendar — GtkCalendar

```
calendar.view
  header.horizontal
    button.image-button        (previous month)
      image
    stack.month                (the header button; its year view stays Avalonia's)
      label
    button.image-button        (next month)
      image
    button.image-button        (previous year; CalendarYears.Step)
      image
    label.year
    button.image-button        (next year)
      image
  grid.horizontal              (AdwHomogeneousGrid, as GtkGrid with homogeneous rows and columns)
    label.day-name …           (abbreviated names)
    label.day-number(.other-month, .today, :selected) …
```

### DatePicker, TimePicker columns — GtkSpinButton, vertical

```
spinbutton.vertical
  button.image-button.up
    image
  text
  button.image-button.down
    image
```

### Other controls

| Avalonia | Node tree |
|---|---|
| Window | `window.background.csd` › `dialog-host` › `widget` › `widget` › `toolbarview` › `revealer.top-bar` › `windowhandle` › `box.vertical` › `headerbar` …, then the content |
| FlyoutPresenter | `popover.background` › `arrow` + `contents` |
| MenuFlyoutPresenter, ContextMenu | `popover.background.menu` › (`arrow` +) `contents` › `scrolledwindow` › `viewport` › `stack` › `box.vertical` › `box.vertical` › `modelbutton` …, as GtkPopoverMenu |
| Menu, MenuItem | `menubar` › `item`; in a popover, `modelbutton.flat` › `box.horizontal` (the `check`/`radio`) + `label` + `accelerator` + `arrow` |
| ScrollViewer, ScrollBar | `scrolledwindow` › `scrollbar.vertical.overlay-indicator` › `range` › `trough` › `slider` |
| ToolTip | `tooltip.background` › `label` |
| CalendarDatePicker | `box.linked.horizontal` › `entry` + `menubutton` › `button.toggle` |
| AutoCompleteBox | `entry`; suggestions `popover.menu` › `contents` › `listview` › `row` |
| NotificationCard, WindowNotificationManager | `toastoverlay` › `toast` › `widget` › `label.heading` + `button.circular.flat` |
| CommandBar | `box.toolbar` › `button.flat` …, `separator`; overflow `menubutton` › `popover.menu` › `modelbutton` |
| PipsPager | `carouselindicatordots` (dots drawn in code) |
| RefreshVisualizer | `image.spinner` (AdwSpinner, drawn in code) |
| Carousel, CarouselPage | `carousel` |
| TransitioningContentControl | `stack` |
| ContentPage | `navigation-view-page` |
| NavigationPage | `navigation-view` › `headerbar` (back button, `label.title`) + pages |
| TabbedPage | `toolbarview` › `viewswitcherbar` › `viewswitcher` › `toggle-group` › `toggle` |
| DrawerPage | `overlay-split-view` with `headerbar`s |
| TextSelectionHandle | `cursor-handle.top`/`.bottom`/`.insertion-cursor` › `contents` |
| SelectableTextBlock | `label` (selection from `AdwSelectionBrush`) |
| ManagedFileChooser | `filechooser` › places `.navigation-sidebar`, location `entry`, `columnview.view` of files |
| PathIcon | `image` |
| ItemsControl, ContentControl, panels, ThemeVariantScope | not nodes |

## 7. Checking a theme

- `tests/AvaAdwaita.Theme.Tests` creates every stock control under
  `AdwaitaTheme` and compares its node tree with the oracle's.
- `tools/shoot.sh` renders the gallery; compare with the same widget in a
  libadwaita app.
- In a debug gallery build, Ctrl+Shift+I shows the node under the pointer,
  its matching rules and its computed values.

## 8. Helpers

- `adw:Css.States="Checked"` adds GTK states no pseudo-class gives, from a style: a split
  button's arrow is `:checked` while its flyout is open, a hyperlink is `:link`.
- `adw:AdwStack.Expand="True"` gives a child of an `AdwStack` the space left over, as
  `hexpand`/`vexpand`.
- `AdwDockLast` docks its last child and fills with the first, for trees where GTK puts
  the docked node last (a notebook with its tabs at the bottom).
- `<adw:LabelTemplate RecognizesAccessKey="True" />` hides a `_` mnemonic, registers the
  access key and underlines it while access keys show, as GtkLabel's `use-underline`.
  `Node=""` makes a label that is no node of its own (the text inside a `label` control).
- Classes starting with `adw-` are the theme's own markers and never reach CSS:
  `adw-no-auto-class` stops GtkButton's automatic `.text-button`/`.image-button`.
- `ItemContainerTheme` swaps the item theme for a variant, as the view switcher does.

## 9. Adding a control to the parity corpus

1. Build the GTK widget in `tools/adw-oracle/adw-oracle.c` (`build()`), and check its tree
   with `adw-oracle --tree TYPE`.
2. Add scenes to `tools/adw-oracle/corpus.py`: the widget, its style classes, the states
   that matter, and whether the accent changes it.
3. Build the Avalonia equivalent in `tests/AvaAdwaita.Theme.Tests/ParityScene.cs`. When GTK's
   widget is a part of the Avalonia control, `ParityScene.Root` picks that part.
   A scene can be a popup: the oracle opens a menu button's popover and records the popover's
   own surface, and `ParityScene.Open` and `Root` open and pick the same popup.
   While working on one control, `tools/regenerate-data.sh --only PREFIX` runs the oracle on
   just the scenes whose ids start with PREFIX and merges them into the data, and
   `AVAADWAITA_SCENES=PREFIX` makes `ParityTests` and `GoldenTests` check just those scenes:
   seconds instead of minutes. Run everything before committing.
4. Run `tools/regenerate-data.sh` (it starts a headless Weston when none is running), then
   `ParityTests` and `GoldenTests`. The reports are in `artifacts/parity` and
   `artifacts/goldens`.

## 10. Other control libraries

A library describes its own controls the way the theme describes Avalonia's, through
`CssNodeProviders`:

```csharp
// Classes from properties, refreshed when a watched property changes.
CssNodeProviders.Register<MyGrid>(g => g.IsCompact ? ["compact"] : [], MyGrid.IsCompactProperty);

// A pseudo-class that maps to a GTK state.
CssNodeProviders.MapPseudoClass<MyHeader>(":sortascending", CssStates.Checked);

// A sibling position from the control rather than the visual order, for panels that keep their children
// in creation order and place them by index.
CssNodeProviders.RegisterPosition<MyRow>(r => (r.Index, r.Owner.Count), Visual.BoundsProperty);
```

`AvaAdwaita.DataGrid` is such a library: see `src/AvaAdwaita.DataGrid`.

Its ControlThemes then set `adw:Css.Node` on the parts that are GTK nodes, as above.
