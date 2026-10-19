using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A stack of pages the user goes forward and back through (AdwNavigationView): a pushed page slides over the last
/// one, and popping it, by a header bar's back button, Escape, Alt+Left or the mouse's back button, slides it away.
/// The first of <see cref="Pages"/> is the root; the others wait to be pushed by tag.
/// </summary>
public class NavigationView : Control
{
    /// <summary>libadwaita's SPRING_STIFFNESS for the transitions.</summary>
    private const double SpringStiffness = 1000;

    /// <summary>Defines <see cref="AnimateTransitions"/>.</summary>
    public static readonly StyledProperty<bool> AnimateTransitionsProperty =
        AvaloniaProperty.Register<NavigationView, bool>(nameof(AnimateTransitions), true);

    /// <summary>Defines <see cref="PopOnEscape"/>.</summary>
    public static readonly StyledProperty<bool> PopOnEscapeProperty =
        AvaloniaProperty.Register<NavigationView, bool>(nameof(PopOnEscape), true);

    /// <summary>Defines <see cref="VisiblePage"/>.</summary>
    public static readonly DirectProperty<NavigationView, NavigationPage?> VisiblePageProperty =
        AvaloniaProperty.RegisterDirect<NavigationView, NavigationPage?>(nameof(VisiblePage), v => v.VisiblePage);

    private readonly ShadowHelper _shadow = new();
    private readonly AdwBox _shield = new() { IsVisible = false };
    private readonly List<NavigationPage> _stack = new();
    private readonly HashSet<NavigationPage> _removeOnPop = new();
    private readonly SpringAnimation _animation;
    private NavigationPage? _showing;
    private NavigationPage? _hiding;
    private bool _transitionPop;
    private double _progress;
    private NavigationPage? _visiblePage;

    /// <summary>Creates a navigation view. It is a <c>navigation-view</c> node.</summary>
    public NavigationView()
    {
        SetValue(Css.NodeProperty, "navigation-view", BindingPriority.Style);
        Css.SetNode(_shield, "widget");
        ClipToBounds = true;
        foreach (var n in _shadow.Nodes)
        {
            VisualChildren.Add(n);
        }

        VisualChildren.Add(_shield);
        Pages.CollectionChanged += OnPagesChanged;
        _animation = new SpringAnimation(this, 1, 1, SpringStiffness, p =>
        {
            _progress = p;
            InvalidateArrange();
        }, OnTransitionDone);
    }

    /// <summary>Raised when a page is pushed.</summary>
    public event EventHandler? Pushed;

    /// <summary>Raised when a page is popped, with the page.</summary>
    public event EventHandler<NavigationPage>? Popped;

    /// <summary>Pages the view holds for pushing by tag; the first is pushed as the root.</summary>
    [Content]
    public AvaloniaList<NavigationPage> Pages { get; } = new();

    /// <summary>Whether pushing and popping slide; otherwise the pages change at once.</summary>
    public bool AnimateTransitions
    {
        get => GetValue(AnimateTransitionsProperty);
        set => SetValue(AnimateTransitionsProperty, value);
    }

    /// <summary>Whether Escape pops the visible page.</summary>
    public bool PopOnEscape
    {
        get => GetValue(PopOnEscapeProperty);
        set => SetValue(PopOnEscapeProperty, value);
    }

    /// <summary>The page on top of the navigation stack.</summary>
    public NavigationPage? VisiblePage
    {
        get => _visiblePage;
        private set => SetAndRaise(VisiblePageProperty, ref _visiblePage, value);
    }

    /// <summary>The navigation stack, from the root.</summary>
    public IReadOnlyList<NavigationPage> NavigationStack => _stack;

    /// <summary>The page under <paramref name="page"/> in the navigation stack, or null.</summary>
    public NavigationPage? GetPreviousPage(NavigationPage page)
    {
        var index = _stack.IndexOf(page);
        return index > 0 ? _stack[index - 1] : null;
    }

    /// <summary>The page with the tag, among the pages and the navigation stack.</summary>
    public NavigationPage? FindPage(string tag) =>
        _stack.LastOrDefault(p => p.NavigationTag == tag) ?? Pages.FirstOrDefault(p => p.NavigationTag == tag);

    /// <summary>Pushes a page; one that is not among <see cref="Pages"/> leaves the view when it is popped.</summary>
    public void Push(NavigationPage page)
    {
        if (_stack.Contains(page))
        {
            return;
        }

        var focus = Focused;
        if (!Pages.Contains(page))
        {
            _removeOnPop.Add(page);
            AddChild(page);
        }

        var old = VisiblePage;
        _stack.Add(page);
        Switch(old, page, pop: false, focus);
        Pushed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Pushes the page with the tag.</summary>
    public void PushByTag(string tag)
    {
        if (FindPage(tag) is { } page)
        {
            Push(page);
        }
    }

    /// <summary>Pops the visible page, if it can be popped; returns whether it was.</summary>
    public bool Pop()
    {
        var page = VisiblePage;
        return page is not null && GetPreviousPage(page) is { } previous && PopTo(previous);
    }

    /// <summary>Pops the pages above <paramref name="page"/>; returns whether any were.</summary>
    public bool PopTo(NavigationPage page)
    {
        var index = _stack.IndexOf(page);
        if (index < 0 || index == _stack.Count - 1 || !_stack[^1].CanPop)
        {
            return false;
        }

        var focus = Focused;
        var old = _stack[^1];
        var popped = _stack.Skip(index + 1).ToList();
        _stack.RemoveRange(index + 1, _stack.Count - index - 1);

        // Pages popped from under the visible one go at once; the visible one slides away.
        foreach (var p in popped.Where(p => p != old))
        {
            FinishHide(p, pop: true);
        }

        Switch(old, page, pop: true, focus);
        foreach (var p in popped)
        {
            Popped?.Invoke(this, p);
        }

        return true;
    }

    /// <summary>Pops to the page with the tag.</summary>
    public bool PopToTag(string tag) => _stack.LastOrDefault(p => p.NavigationTag == tag) is { } page && PopTo(page);

    /// <summary>Replaces the navigation stack with <paramref name="pages"/>, the last of which shows at once, without a transition.</summary>
    public void Replace(IEnumerable<NavigationPage> pages)
    {
        var list = pages.ToList();
        var focus = Focused;
        var old = VisiblePage;
        foreach (var p in _stack.Where(p => !list.Contains(p)).ToList())
        {
            if (p != old)
            {
                FinishHide(p, pop: true);
            }
        }

        _stack.Clear();
        foreach (var p in list)
        {
            if (!Pages.Contains(p) && !VisualChildren.Contains(p))
            {
                _removeOnPop.Add(p);
                AddChild(p);
            }

            _stack.Add(p);
        }

        // adw_navigation_view_replace: replacing the stack has no animation.
        Switch(old, list.LastOrDefault(), pop: true, focus, animate: false);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        if ((e.Key == Key.Escape && PopOnEscape) || (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Alt))
        {
            e.Handled = Pop();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!e.Handled && e.InitialPressMouseButton == MouseButton.XButton1)
        {
            e.Handled = Pop();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.NavigationViewAutomationPeer(this);

    private void OnPagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (NavigationPage page in e.OldItems ?? Array.Empty<NavigationPage>())
        {
            if (!_stack.Contains(page))
            {
                RemoveChild(page);
            }
        }

        foreach (NavigationPage page in e.NewItems ?? Array.Empty<NavigationPage>())
        {
            _removeOnPop.Remove(page);
            if (!VisualChildren.Contains(page))
            {
                AddChild(page);
            }

            // adw_navigation_view_add: the first page is pushed as the root.
            if (_stack.Count == 0)
            {
                _stack.Add(page);
                Switch(null, page, pop: false, null);
            }
        }
    }

    private void AddChild(NavigationPage page)
    {
        page.IsVisible = false;
        VisualChildren.Add(page);
        LogicalChildren.Add(page);
    }

    private void RemoveChild(NavigationPage page)
    {
        VisualChildren.Remove(page);
        LogicalChildren.Remove(page);
    }

    // What has the focus, read before the stack changes: a header bar's back button hides when it does.
    private InputElement? Focused => TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as InputElement;

    private void Switch(NavigationPage? from, NavigationPage? to, bool pop, InputElement? focus, bool animate = true)
    {
        VisiblePage = to;
        if (from == to)
        {
            return;
        }

        // A transition still running ends where it was going.
        if (_hiding is not null || _showing is not null)
        {
            _animation.Jump(1);
        }

        // switch_page: the focus in the page going away is kept for its return, and goes into the page showing.
        var containsFocus = focus is not null && from is not null && from.IsVisualAncestorOf(focus);
        if (containsFocus)
        {
            from!.LastFocus = new WeakReference<InputElement>(focus!);
        }

        if (to is not null)
        {
            // A page shown on top goes last, so it draws over the one it covers.
            VisualChildren.Remove(to);
            VisualChildren.Add(to);
            to.IsVisible = true;
            to.Raise(NavigationPage.ShowingEvent);
            if (containsFocus)
            {
                var last = to.LastFocus is { } weak && weak.TryGetTarget(out var saved) && to.IsVisualAncestorOf(saved) ? saved : null;
                var method = Dialog.FocusMethod(focus);
                if ((last ?? Dialog.FirstFocusable(to)) is { } target)
                {
                    target.Focus(method);
                }
                else
                {
                    // A page shown for the first time has no template yet: its widgets are there once it is laid out.
                    Avalonia.Threading.Dispatcher.UIThread.Post(
                        () =>
                        {
                            if (VisiblePage == to)
                            {
                                Dialog.FirstFocusable(to)?.Focus(method);
                            }
                        },
                        Avalonia.Threading.DispatcherPriority.Loaded);
                }
            }
        }

        from?.Raise(NavigationPage.HidingEvent);
        _hiding = from;
        _showing = to;
        _transitionPop = pop;
        if (animate && from is not null && to is not null && AnimateTransitions && VisualRoot is not null)
        {
            _shield.IsVisible = true;
            _animation.Jump(0);
            _animation.Animate(1);
        }
        else
        {
            _animation.Jump(1);
        }
    }

    // transition_done_cb.
    private void OnTransitionDone()
    {
        if (_progress < 1)
        {
            return;
        }

        if (_hiding is { } hiding)
        {
            _hiding = null;
            FinishHide(hiding, _transitionPop);
        }

        if (_showing is { } showing)
        {
            _showing = null;
            showing.Raise(NavigationPage.ShownEvent);
        }

        _shield.IsVisible = false;
        _shadow.Hide();
        InvalidateArrange();
    }

    private void FinishHide(NavigationPage page, bool pop)
    {
        page.IsVisible = false;
        page.Raise(NavigationPage.HiddenEvent);
        if (pop && _removeOnPop.Remove(page))
        {
            RemoveChild(page);
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = default(Size);
        foreach (var c in VisualChildren.OfType<Control>())
        {
            c.Measure(availableSize);
            if (c is NavigationPage { IsVisible: true })
            {
                size = new Size(Math.Max(size.Width, c.DesiredSize.Width), Math.Max(size.Height, c.DesiredSize.Height));
            }
        }

        return size;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (width, height) = (finalSize.Width, finalSize.Height);
        var full = new Rect(finalSize);
        if (_hiding is null || _showing is null)
        {
            VisiblePage?.Arrange(full);
            VisiblePage?.SetValue(ClipProperty, null);
            _shadow.Hide();
            _shield.Arrange(default);
            return finalSize;
        }

        // adw_navigation_view_size_allocate: the moving page slides over the static one, which is clipped to what shows.
        var (staticPage, movingPage) = _transitionPop ? (_showing, _hiding) : (_hiding, _showing);
        var progress = _transitionPop ? _progress : 1 - _progress;
        var offset = Math.Round(progress * width);
        staticPage.Arrange(full);
        staticPage.Clip = new RectangleGeometry(new Rect(0, 0, offset, height));
        movingPage.Arrange(new Rect(offset, 0, width, height));
        movingPage.Clip = null;
        _shield.Arrange(full);
        _shadow.Arrange(Math.Max(0, offset), height, 0, 0, progress, ShadowDirection.Right);
        return finalSize;
    }
}
