using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using AvaAdwaita.Animation;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A sheet along the bottom of its content, with an optional bar there while it is closed (AdwBottomSheet). The sheet
/// slides up over the dimmed content on a spring as <see cref="IsOpen"/> turns on; clicking the bar opens it, and
/// Escape, a click on the dimmed content or a header bar's close button in the sheet closes it.
/// </summary>
public class BottomSheet : Control
{
    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<BottomSheet, Control?>(nameof(Content));

    /// <summary>Defines <see cref="Sheet"/>.</summary>
    public static readonly StyledProperty<Control?> SheetProperty =
        AvaloniaProperty.Register<BottomSheet, Control?>(nameof(Sheet));

    /// <summary>Defines <see cref="BottomBar"/>.</summary>
    public static readonly StyledProperty<Control?> BottomBarProperty =
        AvaloniaProperty.Register<BottomSheet, Control?>(nameof(BottomBar));

    /// <summary>Defines <see cref="IsOpen"/>.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="Align"/>.</summary>
    public static readonly StyledProperty<double> AlignProperty =
        AvaloniaProperty.Register<BottomSheet, double>(nameof(Align), 0.5, coerce: (_, v) => Math.Clamp(v, 0, 1));

    /// <summary>Defines <see cref="FullWidth"/>.</summary>
    public static readonly StyledProperty<bool> FullWidthProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(FullWidth), true);

    /// <summary>Defines <see cref="ShowDragHandle"/>.</summary>
    public static readonly StyledProperty<bool> ShowDragHandleProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(ShowDragHandle), true);

    /// <summary>Defines <see cref="IsModal"/>.</summary>
    public static readonly StyledProperty<bool> IsModalProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(IsModal), true);

    /// <summary>Defines <see cref="CanOpen"/>.</summary>
    public static readonly StyledProperty<bool> CanOpenProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(CanOpen), true);

    /// <summary>Defines <see cref="CanClose"/>.</summary>
    public static readonly StyledProperty<bool> CanCloseProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(CanClose), true);

    /// <summary>Defines <see cref="RevealBottomBar"/>.</summary>
    public static readonly StyledProperty<bool> RevealBottomBarProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(RevealBottomBar), true);

    /// <summary>Defines <see cref="SheetHeight"/>.</summary>
    public static readonly DirectProperty<BottomSheet, double> SheetHeightProperty =
        AvaloniaProperty.RegisterDirect<BottomSheet, double>(nameof(SheetHeight), s => s.SheetHeight);

    /// <summary>Defines <see cref="BottomBarHeight"/>.</summary>
    public static readonly DirectProperty<BottomSheet, double> BottomBarHeightProperty =
        AvaloniaProperty.RegisterDirect<BottomSheet, double>(nameof(BottomBarHeight), s => s.BottomBarHeight);

    /// <summary>Defines <see cref="CloseAttempt"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CloseAttemptEvent =
        RoutedEvent.Register<BottomSheet, RoutedEventArgs>(nameof(CloseAttempt), RoutingStrategies.Direct);

    private const double TopPaddingMinHeight = 720;
    private const double TopPaddingMinValue = 30;
    private const double TopPaddingTargetHeight = 1440;
    private const double TopPaddingTargetValue = 120;
    private const double ChildSwitchThreshold = 0.15;
    private const double RevealBottomBarDuration = 250;
    private const double SpringDamping = 0.8;
    private const double SpringStiffness = 400;

    private readonly AdwBox _contentBin = Node("widget");
    private readonly AdwBox _dimming = Node("dimming");
    private readonly AdwBox _sheetBin = Node("sheet");
    private readonly AdwBox _outline = Node("outline");
    private readonly SheetPage _page = new();
    private readonly Button _barButton = new() { VerticalAlignment = VerticalAlignment.Top, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly SheetStack _stack;
    private readonly SpringAnimation _open;
    private readonly TimedAnimation _reveal;
    private readonly SwipeTracker _swipe;
    private double _openTarget;
    private double _swipeVelocity;
    private double _swipeDistance = 1;
    private Rect _swipeArea;
    private double _progress;
    private double _revealProgress = 1;
    private bool _showingBottomBar = true;
    private bool _switchChild;
    private bool _useFade;
    private bool _hasBeenOpen;
    private bool _flushLeft;
    private bool _flushRight;
    private double _sheetHeight;
    private double _bottomBarHeight;
    private Action? _closed;
    private IInputElement? _lastContentFocus;

    /// <summary>Creates a bottom sheet: a <c>bottom-sheet</c> node with the content, <c>dimming</c> and <c>sheet</c>.</summary>
    public BottomSheet()
    {
        SetValue(Css.NodeProperty, "bottom-sheet", BindingPriority.Style);
        _stack = new SheetStack(_page, _barButton);
        _dimming.Opacity = 0;
        _dimming.IsVisible = false;
        _outline.IsHitTestVisible = false;
        _sheetBin.Child = new Panel { Children = { _stack, _outline } };
        _sheetBin.IsVisible = false;
        _barButton.IsVisible = false;
        _barButton.Click += (_, _) =>
        {
            if (CanOpen)
            {
                SetCurrentValue(IsOpenProperty, true);
            }
        };
        VisualChildren.Add(_contentBin);
        VisualChildren.Add(_dimming);
        VisualChildren.Add(_sheetBin);
        LogicalChildren.Add(_contentBin);
        LogicalChildren.Add(_sheetBin);
        ClipToBounds = true;
        _dimming.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            if (DimmingClicked is { } clicked)
            {
                clicked();
            }
            else
            {
                RequestClose();
            }
        };
        _sheetBin.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && DimmingClicked is null && !e.Handled)
            {
                e.Handled = true;
                RequestClose();
            }
        };
        _sheetBin.AddHandler(WindowControls.CloseRequestedEvent, (_, e) =>
        {
            if (DimmingClicked is null)
            {
                e.Handled = true;
                RequestClose();
            }
        });
        _open = new SpringAnimation(this, SpringDamping, 1, SpringStiffness, SetProgress, OnOpenDone);
        _reveal = new TimedAnimation(this, RevealBottomBarDuration, AdwEasing.Ease, v =>
        {
            _revealProgress = v;
            if (!RevealBottomBar && v == 0 && _progress == 0)
            {
                _sheetBin.IsVisible = false;
            }

            InvalidateArrange();
        });
        _reveal.Jump(1);

        // The swipe tracker: the sheet follows a drag up or down, from its handle, its header bar or anywhere on it.
        _swipe = new SwipeTracker(this)
        {
            UpperOvershoot = true,
            Distance = () => _swipeDistance,
            Progress = () => _progress,
            SwipeArea = () => _swipeArea,
            Prepare = PrepareSwipe,
            Begin = BeginSwipe,
            Update = v => _open.Set(v),
            End = EndSwipe,
        };
        UpdateSwipeTracker();
        UpdateSheetClasses();
    }

    /// <summary>Raised when the user tries to close the sheet while <see cref="CanClose"/> is false.</summary>
    public event EventHandler<RoutedEventArgs>? CloseAttempt
    {
        add => AddHandler(CloseAttemptEvent, value);
        remove => RemoveHandler(CloseAttemptEvent, value);
    }

    /// <summary>The content, always shown.</summary>
    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The sheet's child, shown while the sheet is open.</summary>
    public Control? Sheet
    {
        get => GetValue(SheetProperty);
        set => SetValue(SheetProperty, value);
    }

    /// <summary>The bar along the bottom while the sheet is closed; clicking it opens the sheet.</summary>
    public Control? BottomBar
    {
        get => GetValue(BottomBarProperty);
        set => SetValue(BottomBarProperty, value);
    }

    /// <summary>Whether the sheet is open.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Where along the bottom a sheet narrower than the control sits, from 0 (the start) to 1 (the end).</summary>
    public double Align
    {
        get => GetValue(AlignProperty);
        set => SetValue(AlignProperty, value);
    }

    /// <summary>Whether the sheet and the bar take the full width rather than their natural width.</summary>
    public bool FullWidth
    {
        get => GetValue(FullWidthProperty);
        set => SetValue(FullWidthProperty, value);
    }

    /// <summary>Whether a drag handle shows at the top of the sheet; a header bar in the sheet then shows no title.</summary>
    public bool ShowDragHandle
    {
        get => GetValue(ShowDragHandleProperty);
        set => SetValue(ShowDragHandleProperty, value);
    }

    /// <summary>Whether the content is dimmed and out of reach while the sheet is open.</summary>
    public bool IsModal
    {
        get => GetValue(IsModalProperty);
        set => SetValue(IsModalProperty, value);
    }

    /// <summary>Whether the user can open the sheet, with the bottom bar.</summary>
    public bool CanOpen
    {
        get => GetValue(CanOpenProperty);
        set => SetValue(CanOpenProperty, value);
    }

    /// <summary>Whether the user can close the sheet; otherwise trying to raises <see cref="CloseAttempt"/>.</summary>
    public bool CanClose
    {
        get => GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    /// <summary>Whether the bottom bar shows; it slides down out of view when not.</summary>
    public bool RevealBottomBar
    {
        get => GetValue(RevealBottomBarProperty);
        set => SetValue(RevealBottomBarProperty, value);
    }

    /// <summary>How much of the sheet or the bar shows above the bottom edge, to keep content clear of it.</summary>
    public double SheetHeight
    {
        get => _sheetHeight;
        private set => SetAndRaise(SheetHeightProperty, ref _sheetHeight, value);
    }

    /// <summary>The bottom bar's height as it shows, 0 without one.</summary>
    public double BottomBarHeight
    {
        get => _bottomBarHeight;
        private set => SetAndRaise(BottomBarHeightProperty, ref _bottomBarHeight, value);
    }

    /// <summary>The sheet node.</summary>
    internal Control SheetNode => _sheetBin;

    /// <summary>The page the sheet's child is in.</summary>
    internal SheetPage Page => _page;

    /// <summary>
    /// Replaces closing on a click on the dimmed content, Escape and the close button, as a dialog's sheet does: the
    /// dialog decides.
    /// </summary>
    internal Action? DimmingClicked { get; set; }

    /// <summary>Opens the sheet, at once without animating, as a dialog does.</summary>
    internal void Open(bool animate = true)
    {
        _closed = null;
        SetCurrentValue(IsOpenProperty, true);
        if (!animate)
        {
            _open.Jump(1);
        }
    }

    /// <summary>Closes the sheet, then calls <paramref name="closed"/>.</summary>
    internal void Close(Action closed)
    {
        _closed = closed;
        if (!IsOpen && !_hasBeenOpen)
        {
            _closed = null;
            closed();
            return;
        }

        SetCurrentValue(IsOpenProperty, false);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.BottomSheetAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            _contentBin.Child = Content;
        }
        else if (change.Property == SheetProperty)
        {
            _page.Child = Sheet;
        }
        else if (change.Property == BottomBarProperty)
        {
            _barButton.Content = BottomBar;
            ShowBottomBar(_showingBottomBar, force: true);
            UpdateSwipeTracker();
            if (_progress == 0)
            {
                _sheetBin.IsVisible = BottomBar is not null && RevealBottomBar;
            }

            InvalidateMeasure();
        }
        else if (change.Property == IsOpenProperty)
        {
            OnOpenChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == ShowDragHandleProperty)
        {
            _page.ShowsHandle = ShowDragHandle;
            UpdateSheetClasses();
            UpdateSwipeTracker();
            InvalidateMeasure();
        }
        else if (change.Property == IsModalProperty)
        {
            if (_progress != 0)
            {
                _dimming.IsVisible = IsModal;
            }
        }
        else if (change.Property == RevealBottomBarProperty)
        {
            if (BottomBar is not null)
            {
                if (RevealBottomBar)
                {
                    _sheetBin.IsVisible = true;
                }

                _reveal.Animate(RevealBottomBar ? 1 : 0);
            }
            else
            {
                _revealProgress = RevealBottomBar ? 1 : 0;
            }

            UpdateSheetClasses();
        }
        else if (change.Property == AlignProperty || change.Property == FullWidthProperty)
        {
            InvalidateArrange();
        }
        else if (change.Property == CanOpenProperty || change.Property == CanCloseProperty)
        {
            UpdateSwipeTracker();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _contentBin.Measure(availableSize);
        _dimming.Measure(availableSize);
        _sheetBin.Measure(availableSize);
        return new Size(
            Math.Max(_contentBin.DesiredSize.Width, Math.Max(_dimming.DesiredSize.Width, _sheetBin.DesiredSize.Width)),
            Math.Max(_contentBin.DesiredSize.Height, Math.Max(_dimming.DesiredSize.Height, _sheetBin.DesiredSize.Height)));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (width, height) = (finalSize.Width, finalSize.Height);
        if (width == 0 && height == 0)
        {
            return finalSize;
        }

        // adw_bottom_sheet_size_allocate. Avalonia mirrors right to left itself, so the align stays as set.
        var extra = SheetExtra();
        var naturalWidth = _stack.Natural(double.PositiveInfinity).Width + extra.Width;
        var sheetWidth = FullWidth ? width : Math.Min(naturalWidth, width);
        var sheetX = Math.Round((width - sheetWidth) * Align);
        var sheetHeight = _stack.Natural(sheetWidth - extra.Width).Height + extra.Height;
        var naturalHeight = sheetHeight;

        var barHeight = 0.0;
        if (BottomBar is not null)
        {
            barHeight = Math.Min(_stack.BarNatural(sheetWidth - extra.Width).Height + extra.Height, height);
            barHeight = Math.Round(barHeight * _revealProgress);
        }

        var topPadding = Math.Truncate(TopPaddingMinValue + ((TopPaddingTargetValue - TopPaddingMinValue)
            * Math.Max(0, (height - TopPaddingMinHeight) / (TopPaddingTargetHeight - TopPaddingMinHeight))));
        sheetHeight = Math.Min(sheetHeight, height - topPadding);
        var progress = _useFade ? (_progress == 0 ? 0 : 1) : _progress;
        var sheetY = height - Math.Round(barHeight + ((sheetHeight - barHeight) * progress));
        SheetHeight = Math.Max(Math.Min(sheetHeight, height - sheetY), barHeight);

        // adw_bottom_sheet_get_distance and get_swipe_area: a drag moves the sheet over its height above the bar, and
        // starts on the part of the sheet that shows.
        _swipeDistance = Math.Max(1, Math.Min(naturalHeight, height) - barHeight);
        _swipeArea = new Rect(sheetX, sheetY, sheetWidth, Math.Max(0, height - sheetY));
        BottomBarHeight = barHeight;
        sheetHeight = Math.Max(sheetHeight, height - sheetY);

        var flushLeft = sheetX == 0;
        var flushRight = sheetX == width - sheetWidth;
        if (flushLeft != _flushLeft || flushRight != _flushRight)
        {
            (_flushLeft, _flushRight) = (flushLeft, flushRight);
            UpdateSheetClasses();
        }

        _contentBin.Arrange(new Rect(finalSize));
        _dimming.Arrange(new Rect(finalSize));
        _sheetBin.Arrange(new Rect(sheetX, sheetY, sheetWidth, sheetHeight));
        if (_page.Child is { } child)
        {
            OverflowClip.Apply(_page, child);
        }

        return finalSize;
    }

    private static AdwBox Node(string name)
    {
        var box = new AdwBox();
        Css.SetNode(box, name);
        return box;
    }

    // What the sheet node adds around its stack: its margin, border and padding.
    private Size SheetExtra() => Css.GetStyle(_sheetBin) is { } s && new CssBoxGeometry(s, default) is var g
        ? new Size(g.Margin.Left + g.Border.Left + g.Padding.Left + g.Margin.Right + g.Border.Right + g.Padding.Right,
            g.Margin.Top + g.Border.Top + g.Padding.Top + g.Margin.Bottom + g.Border.Bottom + g.Padding.Bottom)
        : default;

    private void UpdateSheetClasses() => Css.SetClasses(_sheetBin, "background"
        + (ShowDragHandle ? " has-drag-handle" : string.Empty)
        + (_showingBottomBar && BottomBar is not null ? " bottom-bar" : string.Empty)
        + (RevealBottomBar ? string.Empty : " hidden")
        + (_flushLeft ? " flush-left" : string.Empty)
        + (_flushRight ? " flush-right" : string.Empty));

    private void RequestClose()
    {
        if (!CanClose)
        {
            RaiseEvent(new RoutedEventArgs(CloseAttemptEvent, this));
        }
        else if (IsOpen)
        {
            SetCurrentValue(IsOpenProperty, false);
        }
    }

    // show_bottom_bar: the stack shows the bar's button or the page.
    private void ShowBottomBar(bool show, bool force = false)
    {
        if (show == _showingBottomBar && !force)
        {
            return;
        }

        _showingBottomBar = show;
        var bar = show && BottomBar is not null;
        _barButton.IsVisible = bar;
        _page.IsVisible = !bar;
        UpdateSheetClasses();
    }

    // adw_bottom_sheet_set_open.
    private void OnOpenChanged(bool open)
    {
        if (open)
        {
            _dimming.IsVisible = IsModal;
            _sheetBin.IsVisible = true;
            _hasBeenOpen = true;
        }

        _dimming.IsHitTestVisible = open;
        if (open)
        {
            ShowBottomBar(false);
        }
        else if (_progress < ChildSwitchThreshold)
        {
            ShowBottomBar(true);
        }
        else
        {
            _switchChild = true;
        }

        if (DimmingClicked is null && TopLevel.GetTopLevel(this)?.FocusManager is { } focus)
        {
            // The focus moves into the sheet and back to where it was in the content.
            if (open)
            {
                var focused = focus.GetFocusedElement();
                _lastContentFocus = focused is Visual v && _contentBin.IsVisualAncestorOf(v) ? focused : null;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _sheetBin.Focus(), Avalonia.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                _lastContentFocus?.Focus();
                _lastContentFocus = null;
            }
        }

        // A modal sheet keeps the keyboard out of the content; the dimming keeps the pointer out.
        if (IsModal)
        {
            KeyboardNavigation.SetTabNavigation(_contentBin, open ? KeyboardNavigationMode.None : KeyboardNavigationMode.Continue);
        }

        _useFade = BottomBar is null && !AdwaitaEnvironment.State.AnimationsEnabled;
        _openTarget = open ? 1 : 0;
        _open.Animate(_openTarget, clamp: !open && BottomBar is null, velocity: _swipeVelocity);
        _swipeVelocity = 0;
    }

    // update_swipe_tracker: a pointer drags the sheet while it has a handle or a bar, and the header bars in it then
    // drag the sheet rather than the window.
    private void UpdateSwipeTracker()
    {
        _swipe.IsEnabled = (CanOpen && BottomBar is not null) || CanClose;
        _swipe.AllowMouseDrag = ShowDragHandle || BottomBar is not null;
        _swipe.LowerOvershoot = BottomBar is not null;
        HeaderBarHandle.SetMovesWindow(_sheetBin, !(_swipe.IsEnabled && _swipe.AllowMouseDrag));
    }

    // prepare_cb.
    private bool PrepareSwipe()
    {
        if (BottomBar is null && _open.IsRunning && _openTarget < 0.5)
        {
            return false;
        }

        return IsOpen ? CanClose : CanOpen;
    }

    // begin_swipe_cb.
    private void BeginSwipe()
    {
        _useFade = false;
        _open.Set(_progress);
        if (!IsOpen)
        {
            _dimming.IsVisible = IsModal;
        }
    }

    // end_swipe_cb: past halfway (or flung) the sheet opens or closes, from where the drag left it and as fast;
    // otherwise it goes back.
    private void EndSwipe(double velocity, double to)
    {
        if ((to > 0.5) != IsOpen)
        {
            _swipeVelocity = velocity;
            if (to < 0.5 && DimmingClicked is { } close)
            {
                // A dialog's sheet: the dialog closes, unless it cannot.
                close();
            }
            else
            {
                SetCurrentValue(IsOpenProperty, to > 0.5);
            }

            if ((to > 0.5) == IsOpen)
            {
                return;
            }

            _swipeVelocity = 0;
            to = IsOpen ? 1 : 0;
        }

        _switchChild = true;
        _openTarget = to;
        _open.Animate(to, clamp: to < 0.5 && BottomBar is null, velocity: velocity);
    }

    private void SetProgress(double value)
    {
        var last = _progress;
        _progress = value;
        _dimming.Opacity = Math.Clamp(value, 0, 1);
        if (_useFade)
        {
            _sheetBin.Opacity = Math.Clamp(value, 0, 1);
        }

        if (_switchChild)
        {
            if (last < ChildSwitchThreshold && value >= ChildSwitchThreshold)
            {
                ShowBottomBar(false);
                _switchChild = false;
            }
            else if (last >= ChildSwitchThreshold && value < ChildSwitchThreshold)
            {
                ShowBottomBar(true);
                _switchChild = false;
            }
        }

        InvalidateArrange();
    }

    // open_animation_done_cb.
    private void OnOpenDone()
    {
        if (_progress < 0.5)
        {
            _dimming.IsVisible = false;
            _sheetBin.IsVisible = BottomBar is not null && RevealBottomBar;
            ShowBottomBar(true);
            if (_closed is { } closed)
            {
                _closed = null;
                closed();
            }
        }

        _switchChild = false;
    }
}
