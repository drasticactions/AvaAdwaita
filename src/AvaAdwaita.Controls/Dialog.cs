using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A dialog inside its window (AdwDialog): a sheet over the dimmed window, floating in the middle or, in a narrow window,
/// along the bottom. <see cref="Present"/> shows it in the window that holds a control; Escape and a header bar's close
/// button close it.
/// </summary>
public class Dialog : ContentControl
{
    /// <summary>The width under which <see cref="DialogPresentationMode.Auto"/> is a bottom sheet (max-width: 450px).</summary>
    internal const double PortraitWidth = 450;

    /// <summary>The height under which <see cref="DialogPresentationMode.Auto"/> is a bottom sheet (max-height: 360px).</summary>
    internal const double LandscapeHeight = 360;

    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Dialog, string?>(nameof(Title));

    /// <summary>Defines <see cref="CanClose"/>.</summary>
    public static readonly StyledProperty<bool> CanCloseProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(CanClose), true);

    /// <summary>Defines <see cref="ContentWidth"/>.</summary>
    public static readonly StyledProperty<double> ContentWidthProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(ContentWidth), double.NaN);

    /// <summary>Defines <see cref="ContentHeight"/>.</summary>
    public static readonly StyledProperty<double> ContentHeightProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(ContentHeight), double.NaN);

    /// <summary>Defines <see cref="PresentationMode"/>.</summary>
    public static readonly StyledProperty<DialogPresentationMode> PresentationModeProperty =
        AvaloniaProperty.Register<Dialog, DialogPresentationMode>(nameof(PresentationMode));

    /// <summary>Defines <see cref="Closed"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
        RoutedEvent.Register<Dialog, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="CloseAttempt"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CloseAttemptEvent =
        RoutedEvent.Register<Dialog, RoutedEventArgs>(nameof(CloseAttempt), RoutingStrategies.Direct);

    // What each element under a dialog had for keyboard navigation, and how many dialogs cover it.
    private static readonly Dictionary<InputElement, (int Count, KeyboardNavigationMode Mode)> s_covered = new();

    private readonly ContentPresenter _presenter = new();
    private readonly List<InputElement> _covered = [];
    private readonly AdwBox _bottomBin = new();
    private FloatingSheet? _floating;
    private BottomSheet? _bottom;
    private bool _bottomShown;
    private Panel? _host;
    private IInputElement? _lastFocus;
    private TaskCompletionSource? _shown;
    private bool _closing;

    static Dialog()
    {
        // The breakpoints AdwDialog sets: narrow below 450 wide, short below 360 high.
        CssNodeProviders.Register<Dialog>(
            d =>
            [
                d.IsBottomSheet ? "bottom-sheet" : "floating",
                .. d.Bounds.Width is > 0 and <= PortraitWidth ? ["narrow"] : Array.Empty<string>(),
                .. d.Bounds.Height is > 0 and <= LandscapeHeight ? ["short"] : Array.Empty<string>(),
            ],
            PresentationModeProperty,
            BoundsProperty);
    }

    /// <summary>Creates a dialog.</summary>
    public Dialog()
    {
        // The sheet takes the focus when nothing in it can, and Tab stays in the dialog.
        Focusable = true;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        Css.SetNode(_bottomBin, "widget");
        _presenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(ContentProperty));
        _presenter.Bind(ContentPresenter.ContentTemplateProperty, this.GetObservable(ContentTemplateProperty));
        AddHandler(WindowControls.CloseRequestedEvent, (_, e) =>
        {
            e.Handled = true;
            Close();
        });
    }

    /// <summary>Raised when the dialog has closed and left the window.</summary>
    public event EventHandler<RoutedEventArgs>? Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    /// <summary>Raised when the user tries to close a dialog that cannot close (<see cref="CanClose"/> is false).</summary>
    public event EventHandler<RoutedEventArgs>? CloseAttempt
    {
        add => AddHandler(CloseAttemptEvent, value);
        remove => RemoveHandler(CloseAttemptEvent, value);
    }

    /// <summary>The title a header bar inside the dialog shows.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Whether the user can close the dialog; otherwise trying raises <see cref="CloseAttempt"/>.</summary>
    public bool CanClose
    {
        get => GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    /// <summary>The content's width; NaN takes the content's natural width.</summary>
    public double ContentWidth
    {
        get => GetValue(ContentWidthProperty);
        set => SetValue(ContentWidthProperty, value);
    }

    /// <summary>The content's height; NaN takes the content's natural height.</summary>
    public double ContentHeight
    {
        get => GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }

    /// <summary>Whether the dialog floats or is a bottom sheet.</summary>
    public DialogPresentationMode PresentationMode
    {
        get => GetValue(PresentationModeProperty);
        set => SetValue(PresentationModeProperty, value);
    }

    /// <summary>Whether the dialog shows as a bottom sheet now.</summary>
    internal bool IsBottomSheet => PresentationMode == DialogPresentationMode.BottomSheet
        || (PresentationMode == DialogPresentationMode.Auto && _host is { } host
            && (host.Bounds.Width <= PortraitWidth || host.Bounds.Height <= LandscapeHeight));

    /// <summary>The sheet the dialog shows in.</summary>
    internal Control? Sheet => _bottomShown ? _bottom?.SheetNode : _floating?.Sheet;

    /// <summary>Shows the dialog in the window that holds <paramref name="parent"/>.</summary>
    public void Present(Visual parent)
    {
        if (_host is not null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(parent) ?? throw new InvalidOperationException("The parent is not in a window.");

        // The window's dialog-host, as an AdwWindow's; a top level without one hosts the dialog in its overlay layer.
        _host = top.GetVisualDescendants().OfType<Panel>().FirstOrDefault(p => Css.GetNode(p) == "dialog-host")
            ?? OverlayLayer.GetOverlayLayer(parent)
            ?? throw new InvalidOperationException("The window has nowhere to show a dialog.");
        _lastFocus = top.FocusManager?.GetFocusedElement();
        _closing = false;
        Cover(top);
        _host.Children.Add(this);
        ApplyTemplate();
        UpdateSheet();
        if (_bottomShown)
        {
            _bottom?.Open();
        }
        else
        {
            _floating?.Open();
        }
        Avalonia.Threading.Dispatcher.UIThread.Post(FocusFirst, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>Shows the dialog and completes when it has closed.</summary>
    public Task ShowAsync(Visual parent)
    {
        _shown = new TaskCompletionSource();
        var task = _shown.Task;
        Present(parent);
        return task;
    }

    /// <summary>Closes the dialog, unless it cannot close; returns whether it does.</summary>
    public bool Close()
    {
        if (_host is null)
        {
            return false;
        }

        if (!CanClose)
        {
            RaiseEvent(new RoutedEventArgs(CloseAttemptEvent, this));
            return false;
        }

        ForceClose();
        return true;
    }

    /// <summary>Closes the dialog even if it cannot close.</summary>
    public void ForceClose()
    {
        if (_host is null || _closing)
        {
            return;
        }

        _closing = true;
        if (_bottomShown && _bottom is { } bottom)
        {
            bottom.Close(Remove);
        }
        else if (!_bottomShown && _floating is { } floating)
        {
            floating.Close(Remove);
        }
        else
        {
            Remove();
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _floating = e.NameScope.Find<FloatingSheet>("PART_Floating");
        _bottom = e.NameScope.Find<BottomSheet>("PART_Bottom");
        if (_bottom is not null)
        {
            // As AdwDialog sets its sheet up: as wide as the content, at least 360, with no handle; the dialog closes it.
            _bottom.FullWidth = false;
            _bottom.ShowDragHandle = false;
            _bottom.Page.MinNaturalWidth = 360;
            _bottom.DimmingClicked = () => Close();
        }

        UpdateSheet();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentWidthProperty || change.Property == ContentHeightProperty || change.Property == PresentationModeProperty)
        {
            UpdateSheet();
        }
        else if (change.Property == BoundsProperty && _host is not null && IsBottomSheet != _bottomShown)
        {
            // The window crossed a breakpoint: the dialog moves to the other sheet at once.
            UpdateSheet();
            if (_bottomShown)
            {
                _bottom?.Open(animate: false);
            }
            else
            {
                _floating?.Open();
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.DialogAutomationPeer(this);

    // Only one sheet holds the content: the bottom sheet or the floating one.
    private void UpdateSheet()
    {
        _bottomShown = IsBottomSheet;
        if (_floating is { } floating)
        {
            floating.IsVisible = !_bottomShown;
            floating.ContentWidth = ContentWidth;
            floating.ContentHeight = ContentHeight;
            floating.Child = _bottomShown ? null : _presenter;
            floating.InvalidateMeasure();
        }

        if (_bottom is { } bottom)
        {
            bottom.IsVisible = _bottomShown;
            bottom.Page.ContentWidth = ContentWidth;
            bottom.Page.ContentHeight = ContentHeight;
            _bottomBin.Child = _bottomShown ? _presenter : null;
            bottom.Sheet = _bottomShown ? _bottomBin : null;
            bottom.Page.InvalidateMeasure();
            bottom.InvalidateMeasure();
        }
    }

    private void FocusFirst()
    {
        if (Sheet is { } sheet)
        {
            // ensure_focus: with nothing to focus, the sheet itself takes the focus, so Escape still closes.
            (FirstFocusable(sheet) ?? (InputElement)this).Focus(FocusMethod(_lastFocus as InputElement));
        }
    }

    // adw_dialog_host's present: the window's content, and a dialog under this one, take no keyboard focus while the
    // dialog shows.
    private void Cover(TopLevel top)
    {
        var under = Css.GetNode(_host!) == "dialog-host"
            ? _host!.Children.Where(c => c != this).OfType<InputElement>().ToList()
            : top is ContentControl { Content: InputElement content } ? [content] : [];
        foreach (var element in under)
        {
            s_covered[element] = s_covered.TryGetValue(element, out var c)
                ? (c.Count + 1, c.Mode)
                : (1, KeyboardNavigation.GetTabNavigation(element));
            KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.None);
            _covered.Add(element);
        }
    }

    private void Uncover()
    {
        foreach (var element in _covered)
        {
            if (!s_covered.TryGetValue(element, out var c))
            {
                continue;
            }

            if (c.Count > 1)
            {
                s_covered[element] = (c.Count - 1, c.Mode);
            }
            else
            {
                s_covered.Remove(element);
                KeyboardNavigation.SetTabNavigation(element, c.Mode);
            }
        }

        _covered.Clear();
    }

    // GTK shows the focus ring window-wide once the keyboard moved the focus: a focus moving on keeps it if it had it.
    internal static NavigationMethod FocusMethod(InputElement? previous) =>
        previous is StyledElement { Classes: var classes } && classes.Contains(":focus-visible") ? NavigationMethod.Tab : NavigationMethod.Unspecified;

    // gtk_widget_child_focus forward, as the dialog's first focus: each widget's children by their middles, top to
    // bottom and then start to end (gtk_widget_focus_sort), depth first, so a header bar comes before the content.
    internal static InputElement? FirstFocusable(Visual visual)
    {
        var children = visual.GetVisualChildren()
            .Where(c => c.IsVisible && c is not InputElement { IsEffectivelyEnabled: false })
            .OrderBy(c => c.Bounds.Center.Y)
            .ThenBy(c => c.Bounds.Center.X);
        foreach (var child in children)
        {
            if (child is InputElement { Focusable: true } focusable)
            {
                return focusable;
            }

            if (FirstFocusable(child) is { } inner)
            {
                return inner;
            }
        }

        return null;
    }

    /// <summary>Called when the dialog has closed, before <see cref="Closed"/> is raised.</summary>
    protected virtual void OnClosed()
    {
    }

    private void Remove()
    {
        _host?.Children.Remove(this);
        _host = null;
        _closing = false;
        Uncover();
        _lastFocus?.Focus();
        _lastFocus = null;
        OnClosed();
        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
        _shown?.TrySetResult();
        _shown = null;
    }
}
