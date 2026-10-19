# AvaAdwaita.Controls

`AvaAdwaita.Controls` adds the libadwaita and GTK widgets that Avalonia has no
control for. Each control builds the same CSS node tree as its GTK widget, so
libadwaita's stylesheet draws it, and `ParityTests` compares it with the widget
that `tools/adw-oracle` builds.

```xml
<Application.Styles>
  <adw:AdwaitaTheme />
  <adw:AdwaitaControlsTheme />
</Application.Styles>
```

## Controls

### Rows and preferences

Rows derive from `ListBoxItem` and go in a `ListBox` with the `boxed-list` or
`boxed-list-separate` class, or in a `PreferencesGroup`. They are never
selected, as a boxed list has no selection.

#### ActionRow — AdwActionRow

`Title`, `Subtitle`, `Prefixes` and `Suffixes`. The content is a suffix too,
after `Suffixes`. With `IsActivatable` the row is `.activatable`: a click,
Enter or Space raises `Activated` and runs `Command`.

```xml
<ListBox Classes="boxed-list">
  <adw:ActionRow Title="Wi-Fi" Subtitle="Connected" IsActivatable="True">
    <adw:AdwIcon IconName="go-next-symbolic" />
  </adw:ActionRow>
</ListBox>
```

```
row[.activatable]
  box.header.horizontal
    box.horizontal.prefixes      (when there are prefixes)
    box.vertical.title
      label.title
      label.subtitle             (when there is a subtitle)
    box.horizontal.suffixes      (when there are suffixes)
```

#### SwitchRow — AdwSwitchRow

An activatable action row with a switch as its first suffix. Activating the
row toggles `IsChecked`.

```
row.activatable
  box.header.horizontal
    box.vertical.title
      label.title
    box.horizontal.suffixes
      switch
        image
        image
        slider
```

#### ButtonRow — AdwButtonRow

An activatable row that acts as a button: `Title` in the middle, with
`StartIconName` and `EndIconName`. The `suggested-action` and
`destructive-action` classes color it.

```
row.button.activatable
  box.horizontal
    image.icon.start             (with StartIconName)
    label.title
    image.icon.end               (with EndIconName)
```

#### SpinRow — AdwSpinRow

An activatable action row with a spin button that takes the width beyond the
title. `Value`, `Minimum`, `Maximum`, `Increment` and `FormatString` are the
spin button's. Activating the row focuses the spin button.

```
row.spin.activatable
  box.header.horizontal
    box.vertical.title
      label.title
    box.horizontal.suffixes
      spinbutton.horizontal
        text
        button.image-button.down
          image
        button.image-button.up
          image
```

#### ComboRow — AdwComboRow

An activatable action row that chooses an item from `ItemsSource`: the
selected item shows after the title, and activating the row opens the list in
a popover. `SelectedIndex`, `SelectedItem` and `ItemTemplate` are as a combo
box's. With `UseSubtitle` the selected item is the subtitle instead.

```
row.combo.activatable
  box.header.horizontal
    box.vertical.title
      label.title
    box.horizontal.suffixes
      listview.view.inline
        row.activatable
          box.horizontal
            label
      box.horizontal
        image.dropdown-arrow
```

The popover:

```
popover.background.menu
  contents
    box.vertical
      scrolledwindow
        listview.view
          row.activatable
            box.horizontal
              label
              image              (the selected item's check mark)
```

#### ExpanderRow — AdwExpanderRow

A row whose header expands and collapses `Rows`, its content. `Title`,
`Subtitle`, `Prefixes` and `Suffixes` are the header's. With
`ShowEnableSwitch` a switch sets `EnableExpansion`; turning it on expands the
row, and turning it off collapses it. The row is `:checked` while expanded and
`.empty` while it has no rows.

```
row.expander[.empty]
  box.vertical
    list
      row.header.activatable
        box.header.horizontal
          box.horizontal.prefixes  (when there are prefixes)
            box.horizontal
          box.vertical.title
            label.title
            label.subtitle
          box.horizontal.suffixes
            box.horizontal.suffixes  (when there are suffixes)
            switch                   (with ShowEnableSwitch)
            image.expander-row-arrow
    revealer
      list.nested                (while expanded)
        row …
```

#### EntryRow, PasswordEntryRow — AdwEntryRow, AdwPasswordEntryRow

A row with a text field. While it is empty and not being edited the title is
the placeholder; with text, or while the text has the focus, the title moves
above the text (an animation of 150 ms). `Text`, `IsEditable`, `MaxLength`,
`Prefixes` and `Suffixes`. With `ShowApplyButton`, edited text shows an apply
button, and Enter or the button raises `Applied`; otherwise Enter raises
`EntryActivated`. The row is `.focused` while the text has the focus.

`PasswordEntryRow` shows the text as dots, with a button that sets
`RevealPassword`. Unlike libadwaita, it has no Caps Lock indicator, as
Avalonia does not report the Caps Lock state.

```
row.entry[.password].activatable[.focused]
  box.header.horizontal
    box.horizontal.prefixes      (when there are prefixes)
    widget.editable-area
      label.title.dimmed         (the title while empty)
      label.subtitle             (the title above the text)
      text
      image.edit-icon            (while not editing)
      button.suggested-action.circular.apply-button  (while the text is changed)
    box.horizontal.suffixes      (when there are suffixes; the peek button)
```

#### PreferencesGroup — AdwPreferencesGroup

A `Title`, a `Description` and a `HeaderSuffix` above a boxed list. The rows
among the content go in the list and anything else under it.
`SeparateRows` is the `boxed-list-separate` look. `ItemsSource` and
`ItemTemplate` fill the list from data instead.

```
preferencesgroup
  box.vertical
    box.header.horizontal[.single-line]   (with a title, description or suffix)
      box.vertical.labels
        label.heading.h4
        label.body.dimmed
      …                                  (the header suffix)
    box.vertical
      list.boxed-list[-separate]
        row …
      …                                  (children that are not rows)
```

#### PreferencesPage — AdwPreferencesPage

Its content, preferences groups, in a clamped column that scrolls, under a
`Description` (centered with `DescriptionCentered`) and a `Banner` that does
not scroll. `Title` and `IconName` name the page in a preferences dialog.

```
preferencespage
  [banner]
  scrolledwindow
    viewport
      clamp
        box.vertical.small|medium|large
          label.body.dimmed.description   (with a description)
          preferencesgroup …
```

### Status and feedback

#### LevelBar — GtkLevelBar

A `RangeBase` (0 to 1 by default) shown as one block filled to the value, or
with `Mode="Discrete"` a block per unit. The filled blocks take the class of
the first offset at or above the value: `low`, `high` or `full`, at 0.25, 0.75
and 1; `SetOffset` adds or moves one. `Orientation` and `IsInverted` as GTK's.

```
levelbar.horizontal.continuous|discrete
  trough
    block.filled[.low|.high|.full] …
    block.empty …
```

#### Banner — AdwBanner

A bar with a `Title` and, with `ButtonLabel`, a button (`ButtonStyle`
`Suggested` colors it) that raises `ButtonClicked` and runs `ButtonCommand`.
It slides down when `IsRevealed` turns on. The button sits beside the title
while both fit and moves under it otherwise.

```
banner
  revealer
    widget
      label.heading
      button[.suggested-action]   (with a button label)
```

#### StatusPage — AdwStatusPage

A page that tells the state of a view: `IconName`, `Title` and `Description`,
centered, with the content under them. The `compact` class makes it smaller.

```
statuspage[.compact]
  scrolledwindow
    viewport
      box.vertical
        clamp
          box.vertical
            image.icon           (with an icon)
            box.vertical
              label.title
              label.body.description
        …                        (the content)
```

#### Avatar — AdwAvatar

A circle `Size` wide: `Image` when there is one, else the initials of `Text`
with `ShowInitials`, else `IconName` (adw-avatar-default-symbolic). The color
class comes from the text, as GTK's `g_str_hash`, and is random without
one. Under 25 px the avatar is `.contrasted`.

```
widget
  avatar.color1…14[.contrasted][.image]
    label | image
```

### Layout

#### ToolbarView — AdwToolbarView

`Content` between `TopBars` and `BottomBars`. `TopBarStyle` and
`BottomBarStyle` are `Flat` (with an undershoot when the content scrolls
under the bar), `Raised` or `RaisedBorder`. `RevealTopBars` and
`RevealBottomBars` slide the bars away; `ExtendContentToTopEdge` and
`ExtendContentToBottomEdge` put the content under them. `TopBarHeight` and
`BottomBarHeight` give the height the bars take. As a window's content it
hides the window's own header bar, as an `AdwApplicationWindow`'s content.

```
toolbarview[.undershoot-top][.undershoot-bottom]
  …                              (the content)
  revealer.top-bar[.raised][.border]
    windowhandle
      box.vertical[.collapse-spacing]
        …                        (the top bars)
  revealer.bottom-bar[.raised][.border]
    windowhandle
      box.vertical[.collapse-spacing]
        …                        (the bottom bars)
```

#### ViewStack, ViewSwitcher — AdwViewStack, AdwViewSwitcher

A `ViewStack` holds `ViewStackPage`s, of which `VisiblePage` (or
`VisiblePageName`) shows. A page's `Title`, `IconName`, `BadgeNumber`,
`NeedsAttention` and `IsPageVisible` are for switchers. A `ViewSwitcher` shows
a button of the same width for each page of its `Stack`: `Policy="Wide"` puts
the icon beside the title, for a header bar, and `Narrow` over it.

```
stack
  …                              (the visible page's content)

viewswitcher.wide|narrow
  button.flat.toggle.horizontal|vertical
    stack
      box.horizontal.wide | box.vertical.narrow
        indicatorbin[.badge][.needs-attention]
          mask
          indicator
            label.numeric        (with a badge)
          image
        label
```

#### ViewSwitcherBar — AdwViewSwitcherBar

A narrow `ViewSwitcher` for `Stack` in an action bar, for the bottom of a
narrow window. It slides up when `Reveal` turns on.

```
viewswitcherbar
  actionbar
    revealer
      box
        box.horizontal.start
        viewswitcher.narrow …
        box.horizontal.end
```

#### InlineViewSwitcher — AdwInlineViewSwitcher

A toggle group for the pages of `Stack`, for a view inside the window.
`DisplayMode` shows each page's title (`Labels`), icon (`Icons`) or both;
`CanShrink` lets titles end with an ellipsis.

```
inline-view-switcher
  toggle-group.labels|icons|both
    toggle.toggle
      widget
        indicatorbin             (Labels and Icons; in Both, box.horizontal > indicatorbin + label)
          mask
          indicator
          label | image
    separator.horizontal[.hidden]
    …
```

#### NavigationView, NavigationPage — AdwNavigationView, AdwNavigationPage

A stack of `NavigationPage`s the user goes forward and back through. The first
of `Pages` is the root; `Push`, `PushByTag`, `Pop`, `PopTo`, `PopToTag` and
`Replace` change the stack, and a pushed page slides over the last one on
libadwaita's spring, with the shadow the last one casts. A header bar in a
page shows the page's `Title` and, while there is a page to go back to and the
page `CanPop`, a back button. Escape, Alt+Left and the mouse's back button pop
too. A page pushed without being added to `Pages` leaves the view when it is
popped.

Avalonia has a `NavigationPage` of its own: in C#, a file that uses both
namespaces needs `using NavigationPage = AvaAdwaita.Controls.NavigationPage;`.

```
navigation-view
  navigation-view-page           (the visible page)
    …
```

#### NavigationSplitView — AdwNavigationSplitView

A `Sidebar` page beside a `Content` page, sized as an overlay split view's.
With `IsCollapsed` the two become a navigation view, the content pushed over
the sidebar while `ShowContent`. Each pane's header bar keeps the window
buttons on its outer side.

```
navigation-split-view
  widget.sidebar-pane[.end]
    navigation-view-page …
  widget.content-pane
    navigation-view-page …

navigation-split-view            (collapsed)
  navigation-view
    navigation-view-page …
```

#### OverlaySplitView — AdwOverlaySplitView

A `Sidebar` beside the `Content`. Expanded, the sidebar takes
`SidebarWidthFraction` of the width, between `MinSidebarWidth` and
`MaxSidebarWidth`. With `IsCollapsed` it slides over the content, which it
dims; a click on the content or Escape hides it. Collapsing hides the sidebar
and expanding shows it, unless `PinSidebar`. `SidebarPosition` puts it at the
start or the end. `ShowSidebar` slides it in and out on libadwaita's spring.

```
overlay-split-view
  widget.content-pane            (expanded)
  widget.sidebar-pane[.end]

overlay-split-view               (collapsed, with the sidebar showing)
  dimming.left|right
  shadow.left|right
  border.left|right
  outline.left|right
  widget                         (the content)
  widget                         (catches clicks on the content)
  widget.overlay-pane[.end]
```

#### HeaderBar — AdwHeaderBar

A title bar for a `ToolbarView` or a pane: `Start` widgets from the start,
`End` widgets from the end (the first is the last on the bar, as
`adw_header_bar_pack_end`), the window's title or `TitleWidget` in the middle,
and the window buttons in the user's layout (`ShowStartTitleButtons`,
`ShowEndTitleButtons`). Dragging it moves the window. The window's own header
bar, set with the `WindowHeaderBar` attached properties, builds the same tree.

```
headerbar
  windowhandle
    box
      widget
        box.horizontal.start     (with start widgets or window buttons)
          windowcontrols.start
          …
      widget
        label.title | the title widget
      widget
        box.horizontal.end       (with end widgets or window buttons)
          …
          windowcontrols.end
```

#### WindowTitle — AdwWindowTitle

A header bar's `Title` and `Subtitle`, each on one line with an ellipsis. The
title is at least five characters wide, as GtkLabel's `width-chars`
(`AdwLabel.WidthChars`).

```
windowtitle
  box.vertical
    label.title
    label.subtitle               (with a subtitle)
```

#### Revealer — GtkRevealer

Shows and hides its `Child` with `IsRevealed`: a slide (`SlideDown` by
default, 250 ms) grows the revealer while the child keeps its size, and
`Crossfade` fades it. A hidden child is not shown at all; `IsChildRevealed`
turns on when the child has finished showing. It is a `revealer` node.

#### Clamp — AdwClamp

Keeps its child at most `MaximumSize` (600) wide and centered. From
`TighteningThreshold` (400) up, the child grows more slowly than the clamp, so
the margins appear gradually; the child has the `small`, `medium` or `large`
class for where it is in that range. `Orientation` clamps the height instead.
Sizes are pixels: libadwaita's scale-independent pixels without text scaling.

```
clamp
  … .small|.medium|.large
```

#### WrapBox — AdwWrapBox

A panel that lays its children out in lines, starting a new line when the next
child does not fit: `ChildSpacing` apart on a line and `LineSpacing` between
lines. `Align` places a line that does not fill the box (0 at the start, 1 at
the end); `Justify` instead fills the lines, stretching the children (`Fill`)
or the space between them (`Spread`), but not the last line unless
`JustifyLastLine` is on. Children marked `AdwStack.Expand` take a line's spare
room. `NaturalLineLength` is the box's natural length (by default all the
children on one line), `LineHomogeneous` makes every line as tall as the
tallest, `WrapReverse` stacks the lines upwards and `PackDirection` packs each
line from the end. `Orientation` lays out columns instead. The children's
minimum sizes are their natural ones, so `WrapPolicy` makes no difference.

```
wrap-box
  …                              (the children)
```

### Bars and switchers

#### ActionBar — GtkActionBar

`Start` widgets from the start, `End` widgets from the end (the first is the
last on the bar), and a `CenterWidget`. It slides up when `IsRevealed` turns
on.

```
actionbar
  revealer
    box
      box.horizontal.start
      …                          (the center widget)
      box.horizontal.end
```

#### StackSwitcher — GtkStackSwitcher

Linked toggle buttons for the pages of a `ViewStack`: each shows its page's
icon, or its title when it has none, and the buttons share the width.

```
stackswitcher.linked
  button.text-button.toggle
    label
  button.image-button.toggle
    image
```

### Dialogs

#### Dialog — AdwDialog

A dialog inside its window. `Present(parent)` or `ShowAsync(parent)` shows it
in the dialog host of the window that holds `parent` (another top level's
overlay layer otherwise): a sheet over the dimmed window that grows and fades
in on libadwaita's spring. `ContentWidth` and `ContentHeight` size it (the
content's natural size when NaN); `PresentationMode` is `Auto` (a bottom
sheet in a window narrower than 450 or lower than 360), `Floating` or
`BottomSheet`. A header bar inside takes `Title` and shows only a close
button. Escape and the close button call `Close`, which raises `CloseAttempt`
instead when `CanClose` is false; `ForceClose` closes anyway. `Closed` is
raised when it has left the window, and the focus goes back where it was.

```
dialog-host
  widget                         (the window's content)
  dialog.floating
    widget
      floating-sheet
        dimming
        sheet.background
          widget
            …                    (the content)

  dialog.bottom-sheet[.narrow][.short]
    widget
      bottom-sheet
        widget
        dimming
        sheet.background[.flush-left][.flush-right]
          stack
            widget
              widget
                …                (the content)
          outline
```

A bottom sheet is at least 360 wide and slides up from the bottom; a click on
the dimmed window closes it.

#### PreferencesDialog — AdwPreferencesDialog

A dialog (640 wide by default, titled "Preferences") of `Pages`, each a
`PreferencesPage` named in the view switcher by its `Title` and `IconName`.
The view switcher sits in the header bar while it fits (110pt a page and over
400 wide); otherwise the header bar shows the title and a view switcher bar
shows at the bottom. `VisiblePage` is the page that shows. With
`SearchEnabled`, a search button (or typing) searches the rows of every page
by title and an action row's subtitle; each result names its group and page,
and activating one shows its page with the row focused. `PushSubpage` shows a
`NavigationPage` over the pages, and `PopSubpage` goes back. Toasts are not
supported.

```
dialog.preferences.floating      (or .bottom-sheet, as Dialog)
  …
    toastoverlay
      navigation-view
        navigation-view-page
          widget
            toolbarview
              stack              (the pages, or the search results)
                toolbarview
                  stack
                    preferencespage …
                  revealer.bottom-bar > … > viewswitcherbar
              revealer.top-bar
                … headerbar      (strict centering)
                  widget > box.start > button.toggle  (search)
                  widget > stack > stack > viewswitcher | windowtitle
                                 | clamp > entry.search
```

#### AboutDialog — AdwAboutDialog

A dialog about the application, 360 wide. The main page shows
`ApplicationIcon`, `ApplicationName`, `DeveloperName` and a `Version` button
that copies the version. Below them are rows for what is set: What's New
(`ReleaseNotes` in AppStream markup, under `ReleaseNotesVersion` or the
version), Details (`Comments` in Pango markup and the links from `AddLink`;
without them, `Website` is a row of its own), Support Questions (`SupportUrl`),
Report an Issue (`IssueUrl`), Troubleshooting (`DebugInfo`, to copy or save as
`DebugInfoFilename`), Credits (`Developers`, `Designers`, `Artists`,
`Documenters`, `TranslatorCredits` and `AddCreditSection`), Legal (`Copyright`
and `LicenseType` or a custom `License`, then `AddLegalSection`) and
Acknowledgements (`AddAcknowledgementSection`). Each row opens a page.
A person written "Name <email>" or "Name https://url" gets a link row.
`LinkActivated` is raised for every link before it opens; handling it keeps
it from opening. The application's name shows in the header bar once it has
scrolled out of view. Toasts are not supported.

```
dialog.about.floating            (or .bottom-sheet, as Dialog)
  …
    toastoverlay
      navigation-view
        navigation-view-page
          toolbarview
            scrolledwindow.main-page
              viewport > clamp > box.vertical
                image.icon-dropshadow
                label.title-1
                label
                button.text-button.app-version
                box.vertical
                  preferencesgroup …   (details, support, credits and legal)
            revealer.top-bar > … > headerbar
              widget > revealer > windowtitle
```

#### LinkRow — AdwLinkRow

An activatable `ActionRow` with the external link icon that opens `Uri` when
activated, then is `IsVisited`. `LinkActivated` is raised first; handling it
keeps the link from opening. The about dialog's links are link rows.

```
row.link.activatable[:link|:visited]
  box.header … box.suffixes > image   (adw-external-link-symbolic)
```

#### BottomSheet — AdwBottomSheet

A sheet along the bottom of `Content`, over it when `IsOpen`: `Sheet` is its
child and `BottomBar` an optional bar along the bottom while it is closed,
which opens it when clicked. The sheet slides up on libadwaita's spring and
the content dims behind it (`IsModal`; a click on the dimmed content closes it,
and the keyboard stays out of the content). Escape, the dimming and a header
bar's close button in the sheet close it; with `CanClose` false they raise
`CloseAttempt` instead (`CanOpen` likewise for the bar). `FullWidth` false
makes the sheet and the bar as wide as their content, placed by `Align`;
`ShowDragHandle` shows the handle at the top, and a header bar under it shows
no title. `RevealBottomBar` slides the bar out of view. `SheetHeight` and
`BottomBarHeight` say how much shows above the bottom edge, to keep content
clear of them. The sheet follows a drag, as libadwaita's swipe tracker has it:
down from anywhere on the open sheet, or up from the bar, it opens or closes
when let go past halfway or flung, and goes back otherwise. Touch always drags
it; a mouse does while the handle shows or there is a bottom bar, and then a
header bar in the sheet drags the sheet rather than the window.

```
bottom-sheet
  widget
    …                            (the content)
  dimming                        (while open and modal)
  sheet.background[.has-drag-handle][.bottom-bar][.hidden][.flush-left][.flush-right]
    stack
      widget                     (or, showing the bar: button > the bar)
        …                        (the sheet)
        drag-handle
    outline
```

A dialog shown as a bottom sheet uses one, as wide as its content and at least
360 wide, without the handle.

#### AlertDialog — AdwAlertDialog

A dialog that asks the user to choose: `Heading`, `Body`, an optional
`ExtraChild` under the body, and a button per `Responses` entry
(`AlertDialogResponse` with `Id`, `Label`, `Appearance` of `Default`,
`Suggested` or `Destructive`, and `IsEnabled`; `AddResponse` adds one).
`ChooseAsync(parent)` shows it and returns the id chosen; `Response` is raised
with it. Enter chooses `DefaultResponse`; Escape, or closing the dialog,
chooses `CloseResponse` ("close" by default). It sizes itself as libadwaita
does: at least 300 wide, and at most 372. The responses sit side by side
in equal shares while they fit, and otherwise stack with the first at the
bottom. `PreferWideLayout` lets the dialog widen to keep them side by side,
up to 600. The heading is `title-2` while it fits on one line, else `title-4`.

```
dialog.alert.floating                (or .bottom-sheet, as Dialog)
  …
    sheet.background
      widget
        widget
          windowhandle
            box.vertical
              scrolledwindow.body-scrolled-window.undershoot-bottom
                viewport
                  box.vertical.message-area[.has-heading][.has-body]
                    widget.heading-bin
                      label.title-2 | label.title-4
                    label.body
                    widget.child
              widget.response-area[.compact]
                button.text-button[.suggested-action|.destructive-action]
                  label
```

### Search

#### SearchEntry — GtkSearchEntry

A `TextBox` for search text, with the search icon before the text and a clear
icon after it while there is text; its natural width keeps room for the clear
icon either way. `SearchChanged` is raised `SearchDelay` milliseconds (150)
after the text stops changing, and at once when it is cleared; `Activated` is
raised on Enter and `StopSearch` on Escape.

```
entry.search
  image                          (system-search-symbolic)
  text
    placeholder
  image                          (edit-clear-symbolic, while there is text)
```

#### SearchBar — GtkSearchBar

A bar that slides down with its `Content` centered, usually a `SearchEntry`
(the bar searches with `Entry`, or the first text box in the content). Turning
`IsSearchMode` on reveals it and focuses the entry; Escape in the entry or the
close button (`ShowCloseButton`) hides it and clears the entry. Text typed in
`KeyCaptureTarget` while the bar is hidden opens it with that text.

```
searchbar
  revealer
    box
      …                          (the content)
      button.image-button.close  (ShowCloseButton)
        image
```

### Shortcuts

#### ShortcutLabel — AdwShortcutLabel

Shows a keyboard shortcut as keycaps. `Accelerator` takes GTK's accelerator
syntax: `<Control>q` is one shortcut (`<Primary>` is Control), spaces separate
alternatives (shown with "/"), `&` joins keys pressed together, `+` joins the
steps of a sequence (shown with "→") and `...` makes a range (shown with "⋯").
Modifiers come in libadwaita's order (Hyper, Super, Ctrl, Alt, Shift, Meta),
and keys show as GTK labels them: the character in upper case, an arrow or a
symbol, or the key's name in GTK's "keyboard label" translations. With no
accelerator the label shows `DisabledText`. Its accessible name is the
shortcut as a menu shows it, such as "Shift+Ctrl+Z".

```
shortcut-label
  box.horizontal                 (a shortcut)
    label.keycap …
  label.dimmed                   ("/", "⋯", "→", or the disabled text)
```

#### ShortcutsDialog — AdwShortcutsDialog

A dialog listing keyboard shortcuts, titled "Keyboard Shortcuts". Each
`ShortcutsSection` in `Sections` (with `Title`) is a preferences group of its
`Items`; an untitled section continues the titled one before it. Each
`ShortcutsItem` has a `Title`, an optional `Subtitle`, an `Accelerator` in
`ShortcutLabel`'s syntax ("No Shortcut" when empty) and a `Direction`, so a
shortcut can show only in left-to-right or right-to-left text. The search
entry in the header bar finds shortcuts by title or subtitle; activating a
result goes back to the shortcut's row. Typing or Ctrl+F starts a search. With
at least four titled sections and twenty shortcuts, toggle buttons at the top
show one section at a time. Escape unselects the section, ends the search, or
closes the dialog. As libadwaita does, the dialog takes its content's height
when it shows, whatever `ContentHeight` was before; set it afterwards for a
fixed height. libadwaita's action names, which take an application's
accelerators, have no counterpart.

```
dialog.shortcuts
  … sheet
    toolbarview
      revealer.top-bar … headerbar
        … clamp > entry.search
      stack
        preferencespage.contents
          … box.vertical
            preferencesgroup     (the section buttons)
              … wrap-box.navigation-box > button.toggle …
            preferencesgroup …   (a section)
              … list.boxed-list
                row.shortcut-row
                  wrap-box
                    box.vertical.title-box
                      label.title
                      label.subtitle
                    shortcut-label …
        preferencespage          (the results)
          … list.boxed-list > row.shortcut-row.activatable …
        statuspage               (no results)
```

### Tabs

#### TabView — AdwTabView

Pages shown one at a time, for a `TabBar` (or a tab overview) to switch
between. Each `TabPage` holds `Content` and what its tab shows: `Title`,
`Tooltip`, `IconName`, `IsLoading`, an `IndicatorIconName` (activatable with
`IsIndicatorActivatable`, raising `IndicatorActivated`) and `NeedsAttention`.
`Pages` lists them, pinned ones first; `Append`, `Prepend`, `Insert` and their
`…Pinned` forms add pages, and `AddPage(content, parent)` adds one after the
page it was opened from, which closing it goes back to. `SelectedPage` is the
page that shows. `SetPagePinned`, `ReorderPage` (and `ReorderBackward`,
`ReorderForward`, `ReorderFirst`, `ReorderLast`) and `TransferPage` move pages.
`ClosePage` raises `ClosePageRequested`: handle it to decide, calling
`ClosePageFinish` then or later. Otherwise the page closes unless it is
pinned. `Shortcuts` work anywhere in the window: Ctrl+Tab, Ctrl+Shift+Tab,
Ctrl+Page Up and Down, Ctrl+Home and End, the same with Shift to move the
page, and Alt+1 to Alt+0. `Menu` is the tab context menu; `SetupMenu` is
raised with its page before it opens.

```
tabview
  widget                         (each page; only the selected one shows)
    …                            (the content)
```

#### TabBar — AdwTabBar

The tabs of a `View`: the pinned tabs, then the others sharing the rest of
the width 5 px apart, up to 220 px each without `ExpandTabs`. A lone unpinned
tab takes the `single-tab` style. Clicking a tab selects its page,
middle-clicking closes it, its close button (shown on hover and while
selected) closes it, dragging moves it, and a context click or Shift+F10
opens the view's `Menu`. With the keyboard focus on a tab, the arrows, Home
and End move between pages, and with Shift they move the page. Tabs open and
close animating their width; while the pointer is over the bar, closing a tab
keeps the others' widths until the pointer leaves. The tabs scroll with the
wheel when they do not fit, and an edge marks a tab needing attention that is
scrolled past it. `IsInverted` puts the close buttons first.
`StartActionWidget` and `EndActionWidget` go at either end. With `Autohide`
the bar shows only for two or more pages, or a pinned one. Dragging tabs
between windows is not supported.

```
tabbar
  revealer
    box.box.horizontal
      widget.start-action
      scrolledwindow.pinned
        tabbox
          separator.vertical[.hidden]
          tabboxchild
            tab.pinned[:selected][.needs-attention]
              image
              indicator
          revealer > indicator.left
          revealer > indicator.right
      scrolledwindow
        tabbox[.single-tab]
          separator.vertical[.hidden]
          tabboxchild
            tab[:selected][.needs-attention][.loading]
              image
              widget.tab-title > label
              button.flat.image-button.tab-indicator > image
              button.flat.image-button.tab-close-button > image
              indicator
          revealer > indicator.left
          revealer > indicator.right
      widget.end-action
```

#### TabButton — AdwTabButton

A button showing how many pages its `View` has: the count on the tab icon,
smaller from 10 pages, and an overflow icon from 100. It is marked when a page
other than the selected one needs attention. `Click` and `Command` usually
open a `TabOverview`.

```
tabbutton
  button.image-button
    indicatorbin
      mask
      indicator
      overlay
        image                    (adw-tab-counter-symbolic)
        label.numeric[.small]
```

#### TabOverview — AdwTabOverview

A grid of the `View`'s pages, shown over the overview's `Content`, which holds
the view (usually with a tab bar). `IsOpen`, `Open` and `Close` show and hide
it. Opening zooms the selected page out into its thumbnail. Clicking a
thumbnail (or Enter on it) shows that page and closes the overview;
middle-clicking closes the page, dragging moves it, and the arrow keys move
between thumbnails. A thumbnail shows its page as it was when the overview
opened, or the view's default icon for a page that has not shown yet. The
pinned pages have a grid of their own above. The header bar shows the page
count and, with `EnableSearch`, searches the pages by title, tooltip and
`Keyword` (Ctrl+F, or typing). `EnableNewTab` adds a button that raises
`CreateTab`: add a page and set it in the arguments. Escape ends the search,
then closes the overview. `SecondaryMenu` goes at the header bar's end.

```
taboverview
  widget[.background]            (the content)
  toolbarview.overview           (while open)
    overlay
      scrolledwindow
        widget
          tabgrid                (pinned pages)
            tabgridchild > tabthumbnail.pinned
              box.vertical > overlay.thumbnail
                overlay.card
                  image.tab-unpin-icon
                  widget.pinned-box > box.horizontal.icon-title-box …
                revealer.needs-attention
          tabgrid
            tabgridchild > tabthumbnail
              box.vertical
                overlay.thumbnail
                  overlay.card
                    picture
                    button.tab-close-button > image
                  revealer.needs-attention > widget
                box.horizontal.icon-title-box
                  image.tab-icon
                  widget > label
      statuspage …               (no pages, or none found)
      button.pill.suggested-action.new-tab-button
        buttoncontent > box.horizontal > image + label
    revealer.top-bar
      … headerbar                (search, the page count, the menu)
      searchbar …
```
