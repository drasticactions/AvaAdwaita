using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.VisualTree;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A sidebar page beside a content page (AdwNavigationSplitView), as a mail app's folders and messages. Collapsed, the
/// two become a navigation view, the content pushed over the sidebar while <see cref="ShowContent"/>.
/// </summary>
public class NavigationSplitView : Control
{
    /// <summary>Defines <see cref="Sidebar"/>.</summary>
    public static readonly StyledProperty<NavigationPage?> SidebarProperty =
        AvaloniaProperty.Register<NavigationSplitView, NavigationPage?>(nameof(Sidebar));

    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<NavigationPage?> ContentProperty =
        AvaloniaProperty.Register<NavigationSplitView, NavigationPage?>(nameof(Content));

    /// <summary>Defines <see cref="IsCollapsed"/>.</summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationSplitView, bool>(nameof(IsCollapsed));

    /// <summary>Defines <see cref="ShowContent"/>.</summary>
    public static readonly StyledProperty<bool> ShowContentProperty =
        AvaloniaProperty.Register<NavigationSplitView, bool>(nameof(ShowContent), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="SidebarPosition"/>.</summary>
    public static readonly StyledProperty<PackType> SidebarPositionProperty =
        OverlaySplitView.SidebarPositionProperty.AddOwner<NavigationSplitView>();

    /// <summary>Defines <see cref="MinSidebarWidth"/>.</summary>
    public static readonly StyledProperty<double> MinSidebarWidthProperty =
        OverlaySplitView.MinSidebarWidthProperty.AddOwner<NavigationSplitView>();

    /// <summary>Defines <see cref="MaxSidebarWidth"/>.</summary>
    public static readonly StyledProperty<double> MaxSidebarWidthProperty =
        OverlaySplitView.MaxSidebarWidthProperty.AddOwner<NavigationSplitView>();

    /// <summary>Defines <see cref="SidebarWidthFraction"/>.</summary>
    public static readonly StyledProperty<double> SidebarWidthFractionProperty =
        OverlaySplitView.SidebarWidthFractionProperty.AddOwner<NavigationSplitView>();

    private readonly AdwBox _sidebarBin = Bin("sidebar-pane");
    private readonly AdwBox _contentBin = Bin("content-pane");
    private NavigationView? _view;
    private bool _changing;

    static NavigationSplitView()
    {
        AffectsMeasure<NavigationSplitView>(MinSidebarWidthProperty, MaxSidebarWidthProperty, SidebarWidthFractionProperty);
    }

    /// <summary>Creates a split view. It is a <c>navigation-split-view</c> node.</summary>
    public NavigationSplitView()
    {
        SetValue(Css.NodeProperty, "navigation-split-view", BindingPriority.Style);
        UpdateCollapsed();
    }

    private static AdwBox Bin(string classes)
    {
        var bin = new AdwBox();
        Css.SetNode(bin, "widget");
        Css.SetClasses(bin, classes);
        return bin;
    }

    /// <summary>The sidebar page.</summary>
    public NavigationPage? Sidebar
    {
        get => GetValue(SidebarProperty);
        set => SetValue(SidebarProperty, value);
    }

    /// <summary>The content page.</summary>
    public NavigationPage? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Whether the pages are one navigation view rather than side by side.</summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Whether the collapsed view shows the content rather than the sidebar.</summary>
    public bool ShowContent
    {
        get => GetValue(ShowContentProperty);
        set => SetValue(ShowContentProperty, value);
    }

    /// <summary>Which side the sidebar is on.</summary>
    public PackType SidebarPosition
    {
        get => GetValue(SidebarPositionProperty);
        set => SetValue(SidebarPositionProperty, value);
    }

    /// <summary>The sidebar's smallest width.</summary>
    public double MinSidebarWidth
    {
        get => GetValue(MinSidebarWidthProperty);
        set => SetValue(MinSidebarWidthProperty, value);
    }

    /// <summary>The sidebar's largest width.</summary>
    public double MaxSidebarWidth
    {
        get => GetValue(MaxSidebarWidthProperty);
        set => SetValue(MaxSidebarWidthProperty, value);
    }

    /// <summary>The share of the width the sidebar takes, between its smallest and largest widths.</summary>
    public double SidebarWidthFraction
    {
        get => GetValue(SidebarWidthFractionProperty);
        set => SetValue(SidebarWidthFractionProperty, value);
    }

    /// <summary>The navigation view while collapsed.</summary>
    internal NavigationView? NavigationView => _view;

    /// <summary>Whether <paramref name="visual"/> is in the sidebar.</summary>
    internal bool IsInSidebar(Visual visual) => Sidebar is { } sidebar && (sidebar == visual || sidebar.IsVisualAncestorOf(visual));

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCollapsedProperty || change.Property == SidebarProperty || change.Property == ContentProperty
            || change.Property == SidebarPositionProperty)
        {
            UpdateCollapsed();
        }
        else if (change.Property == ShowContentProperty && !_changing)
        {
            _changing = true;
            try
            {
                UpdateStack(changing: true);
            }
            finally
            {
                _changing = false;
            }
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.NavigationSplitViewAutomationPeer(this);

    // update_collapsed: the pages move between the panes and the navigation view.
    private void UpdateCollapsed()
    {
        _sidebarBin.Child = null;
        _contentBin.Child = null;
        if (_view is not null)
        {
            _view.PropertyChanged -= OnViewPropertyChanged;
            _view.Replace([]);

            // One at a time: a reset would not tell the view which pages to let go of.
            foreach (var page in new List<NavigationPage>(_view.Pages))
            {
                _view.Pages.Remove(page);
            }
            VisualChildren.Remove(_view);
            LogicalChildren.Remove(_view);
            _view = null;
        }

        VisualChildren.Clear();
        LogicalChildren.Clear();
        if (IsCollapsed)
        {
            _view = new NavigationView();
            VisualChildren.Add(_view);
            LogicalChildren.Add(_view);
            var pages = SidebarPosition == PackType.End ? new[] { Content, Sidebar } : new[] { Sidebar, Content };
            foreach (var page in pages)
            {
                if (page is not null)
                {
                    _view.Pages.Add(page);
                }
            }

            UpdateStack(changing: false);
            _view.PropertyChanged += OnViewPropertyChanged;
        }
        else
        {
            Css.SetClasses(_sidebarBin, SidebarPosition == PackType.End ? "end sidebar-pane" : "sidebar-pane");
            VisualChildren.Add(_sidebarBin);
            VisualChildren.Add(_contentBin);
            LogicalChildren.Add(_sidebarBin);
            LogicalChildren.Add(_contentBin);
            foreach (var page in new[] { Sidebar, Content })
            {
                if (page is not null)
                {
                    page.IsVisible = true;
                }
            }

            _sidebarBin.Child = Sidebar;
            _contentBin.Child = Content;
        }

        InvalidateMeasure();
    }

    // update_navigation_stack: the sidebar, with the content over it while it shows (the other way round at the end).
    private void UpdateStack(bool changing)
    {
        if (_view is null)
        {
            return;
        }

        var (sidebar, content) = (Sidebar, Content);
        if (changing && sidebar is not null && content is not null)
        {
            var atEnd = SidebarPosition == PackType.End;
            if (ShowContent != atEnd)
            {
                _view.Replace([atEnd ? content : sidebar]);
                _view.Push(atEnd ? sidebar : content);
            }
            else
            {
                _view.Replace(atEnd ? [content, sidebar] : [sidebar, content]);
                _view.Pop();
            }

            return;
        }

        var stack = new List<NavigationPage>();
        if (SidebarPosition == PackType.End)
        {
            if (content is not null)
            {
                stack.Add(content);
            }

            if (sidebar is not null && (!ShowContent || content is null))
            {
                stack.Add(sidebar);
            }
        }
        else
        {
            if (sidebar is not null)
            {
                stack.Add(sidebar);
            }

            if (content is not null && (ShowContent || sidebar is null))
            {
                stack.Add(content);
            }
        }

        _view.Replace(stack);
    }

    // notify_visible_page_cb: popping the content in the navigation view hides it.
    private void OnViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != NavigationView.VisiblePageProperty || _changing || Sidebar is null || Content is null)
        {
            return;
        }

        _changing = true;
        try
        {
            SetCurrentValue(ShowContentProperty, _view?.VisiblePage == Content);
        }
        finally
        {
            _changing = false;
        }
    }

    private (double Min, double Max) SidebarRange(double width)
    {
        var min = Math.Ceiling(MinSidebarWidth);
        return (min, Math.Min(Math.Max(min, Math.Ceiling(MaxSidebarWidth)), width));
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_view is not null)
        {
            _view.Measure(availableSize);
            return _view.DesiredSize;
        }

        _sidebarBin.Measure(availableSize);
        _contentBin.Measure(availableSize);
        var content = _contentBin.DesiredSize;
        var (min, max) = SidebarRange(double.PositiveInfinity);
        var natural = Math.Clamp(Math.Ceiling(content.Width * SidebarWidthFraction / (1.0 - SidebarWidthFraction)), min, max);
        return new Size(natural + content.Width, Math.Max(_sidebarBin.DesiredSize.Height, content.Height));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_view is not null)
        {
            _view.Arrange(new Rect(finalSize));
            return finalSize;
        }

        // allocate_uncollapsed; Avalonia mirrors a right-to-left view itself.
        var width = finalSize.Width;
        var (min, max) = SidebarRange(width);
        var sidebar = Math.Clamp(Math.Floor(width * SidebarWidthFraction), min, Math.Max(min, max));
        if (SidebarPosition == PackType.Start)
        {
            _sidebarBin.Arrange(new Rect(0, 0, sidebar, finalSize.Height));
            _contentBin.Arrange(new Rect(sidebar, 0, width - sidebar, finalSize.Height));
        }
        else
        {
            _sidebarBin.Arrange(new Rect(width - sidebar, 0, sidebar, finalSize.Height));
            _contentBin.Arrange(new Rect(0, 0, width - sidebar, finalSize.Height));
        }

        return finalSize;
    }
}
