using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A sidebar beside the content (AdwOverlaySplitView). Expanded, the sidebar takes a share of the width; collapsed, it
/// slides over the content, which it dims, and a click on the content or Escape hides it.
/// </summary>
public class OverlaySplitView : Control
{
    /// <summary>Defines <see cref="Sidebar"/>.</summary>
    public static readonly StyledProperty<object?> SidebarProperty =
        AvaloniaProperty.Register<OverlaySplitView, object?>(nameof(Sidebar));

    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<OverlaySplitView>();

    /// <summary>Defines <see cref="IsCollapsed"/>.</summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<OverlaySplitView, bool>(nameof(IsCollapsed));

    /// <summary>Defines <see cref="ShowSidebar"/>.</summary>
    public static readonly StyledProperty<bool> ShowSidebarProperty =
        AvaloniaProperty.Register<OverlaySplitView, bool>(nameof(ShowSidebar), true, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="PinSidebar"/>.</summary>
    public static readonly StyledProperty<bool> PinSidebarProperty =
        AvaloniaProperty.Register<OverlaySplitView, bool>(nameof(PinSidebar));

    /// <summary>Defines <see cref="SidebarPosition"/>.</summary>
    public static readonly StyledProperty<PackType> SidebarPositionProperty =
        AvaloniaProperty.Register<OverlaySplitView, PackType>(nameof(SidebarPosition));

    /// <summary>Defines <see cref="MinSidebarWidth"/>.</summary>
    public static readonly StyledProperty<double> MinSidebarWidthProperty =
        AvaloniaProperty.Register<OverlaySplitView, double>(nameof(MinSidebarWidth), 180);

    /// <summary>Defines <see cref="MaxSidebarWidth"/>.</summary>
    public static readonly StyledProperty<double> MaxSidebarWidthProperty =
        AvaloniaProperty.Register<OverlaySplitView, double>(nameof(MaxSidebarWidth), 280);

    /// <summary>Defines <see cref="SidebarWidthFraction"/>.</summary>
    public static readonly StyledProperty<double> SidebarWidthFractionProperty =
        AvaloniaProperty.Register<OverlaySplitView, double>(nameof(SidebarWidthFraction), 0.25);

    private readonly ShadowHelper _shadow = new();
    private readonly AdwBox _contentBin = Bin();
    private readonly AdwBox _shield = Bin();
    private readonly AdwBox _sidebarBin = Bin();
    private readonly ContentPresenter _contentPresenter = new();
    private readonly ContentPresenter _sidebarPresenter = new();
    private readonly SpringAnimation _animation;
    private double _progress = 1;
    private double _sidebarWidth;

    static OverlaySplitView()
    {
        AffectsMeasure<OverlaySplitView>(IsCollapsedProperty, SidebarPositionProperty, MinSidebarWidthProperty, MaxSidebarWidthProperty, SidebarWidthFractionProperty);
        ClipToBoundsProperty.OverrideDefaultValue<OverlaySplitView>(true);
    }

    /// <summary>Creates a split view. It is an <c>overlay-split-view</c> node.</summary>
    public OverlaySplitView()
    {
        SetValue(Css.NodeProperty, "overlay-split-view", BindingPriority.Style);
        _contentBin.Child = _contentPresenter;
        _sidebarBin.Child = _sidebarPresenter;
        _shield.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            ShowSidebar = false;
        };

        // The shadow helper's nodes come first, as AdwShadowHelper adds them; then the content, the shield and the sidebar.
        foreach (var n in _shadow.Nodes)
        {
            VisualChildren.Add(n);
        }

        VisualChildren.Add(_contentBin);
        VisualChildren.Add(_shield);
        VisualChildren.Add(_sidebarBin);
        LogicalChildren.Add(_contentBin);
        LogicalChildren.Add(_sidebarBin);
        _animation = new SpringAnimation(this, 1, 0.5, 500, SetProgress, () =>
        {
            if (_progress < 0.5)
            {
                _sidebarBin.IsVisible = false;
            }
        });
        UpdateCollapsed();
        UpdateShield();
    }

    private static AdwBox Bin()
    {
        var bin = new AdwBox();
        Css.SetNode(bin, "widget");
        return bin;
    }

    /// <summary>The sidebar.</summary>
    public object? Sidebar
    {
        get => GetValue(SidebarProperty);
        set => SetValue(SidebarProperty, value);
    }

    /// <summary>The content.</summary>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Whether the sidebar slides over the content rather than taking its own share of the width.</summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Whether the sidebar shows. Collapsing hides it and expanding shows it, unless <see cref="PinSidebar"/>.</summary>
    public bool ShowSidebar
    {
        get => GetValue(ShowSidebarProperty);
        set => SetValue(ShowSidebarProperty, value);
    }

    /// <summary>Keeps <see cref="ShowSidebar"/> as it is when the view collapses or expands.</summary>
    public bool PinSidebar
    {
        get => GetValue(PinSidebarProperty);
        set => SetValue(PinSidebarProperty, value);
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

    /// <summary>The share of the width the expanded sidebar takes, between its smallest and largest widths.</summary>
    public double SidebarWidthFraction
    {
        get => GetValue(SidebarWidthFractionProperty);
        set => SetValue(SidebarWidthFractionProperty, value);
    }

    /// <summary>Whether <paramref name="visual"/> is in the sidebar.</summary>
    internal bool IsInSidebar(Visual visual) => _sidebarBin.IsVisualAncestorOf(visual);

    /// <summary>How far the sidebar shows, 0 to 1 (more while a spring overshoots).</summary>
    internal double Progress => _progress;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            _contentPresenter.Content = change.NewValue;
        }
        else if (change.Property == SidebarProperty)
        {
            _sidebarPresenter.Content = change.NewValue;
        }
        else if (change.Property == ShowSidebarProperty)
        {
            var show = change.GetNewValue<bool>();
            if (show)
            {
                _sidebarBin.IsVisible = true;
            }

            if (VisualRoot is not null)
            {
                _animation.Animate(show ? 1 : 0, clamp: !show);
            }
            else
            {
                _animation.Jump(show ? 1 : 0);
                _sidebarBin.IsVisible = show;
            }
        }
        else if (change.Property == IsCollapsedProperty)
        {
            // adw_overlay_split_view_set_collapsed: collapsing hides the sidebar and expanding shows it, at once.
            UpdateCollapsed();
            UpdateShield();
            if (!PinSidebar)
            {
                var show = !change.GetNewValue<bool>();
                SetCurrentValue(ShowSidebarProperty, show);
                _animation.Jump(show ? 1 : 0);
                _sidebarBin.IsVisible = show;
            }
        }
        else if (change.Property == SidebarPositionProperty)
        {
            UpdateCollapsed();
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && IsCollapsed && _progress > 0 && !e.Handled)
        {
            e.Handled = true;
            ShowSidebar = false;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.OverlaySplitViewAutomationPeer(this);

    // Avalonia mirrors a right-to-left view itself, so the sidebar is on the left unless it is at the end.
    private bool AtStart => SidebarPosition == PackType.Start;

    private void SetProgress(double progress)
    {
        _progress = progress;
        UpdateShield();
        InvalidateMeasure();
    }

    // update_shield: the shield catches clicks on the content while a collapsed sidebar shows.
    private void UpdateShield() => _shield.IsVisible = IsCollapsed && _progress > 0;

    // update_collapsed: the bins' classes.
    private void UpdateCollapsed()
    {
        Css.SetClasses(_contentBin, IsCollapsed ? null : "content-pane");
        var sidebar = IsCollapsed ? "overlay-pane" : "sidebar-pane";
        Css.SetClasses(_sidebarBin, SidebarPosition == PackType.End ? "end " + sidebar : sidebar);
        if (!IsCollapsed)
        {
            _shadow.Hide();
        }
    }

    private (double Min, double Max) SidebarRange()
    {
        var min = Math.Max(0, Math.Ceiling(MinSidebarWidth));
        return (min, Math.Max(min, Math.Ceiling(MaxSidebarWidth)));
    }

    // get_sidebar_width.
    private double SidebarWidth(double width, bool collapsed)
    {
        var (min, max) = SidebarRange();
        return collapsed ? Math.Clamp(width, min, max) : Math.Clamp(Math.Floor(width * SidebarWidthFraction), min, max);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var n in _shadow.Nodes)
        {
            n.Measure(Size.Infinity);
        }

        _shield.Measure(availableSize);
        _contentBin.Measure(availableSize);
        _sidebarBin.Measure(availableSize);
        var content = _contentBin.DesiredSize;
        var sidebar = _sidebarBin.IsVisible ? _sidebarBin.DesiredSize : default;
        var (min, max) = SidebarRange();
        if (IsCollapsed)
        {
            return new Size(Math.Max(content.Width, Math.Clamp(sidebar.Width, min, max)), Math.Max(content.Height, sidebar.Height));
        }

        // measure_uncollapsed: the sidebar's natural width comes from the content's and the fraction.
        var natural = Math.Clamp(Math.Ceiling(content.Width * SidebarWidthFraction / (1.0 - SidebarWidthFraction)), min, max);
        var width = Math.Floor(natural * Math.Clamp(_progress, 0, 1)) + content.Width;
        return new Size(width, Math.Max(content.Height, sidebar.Height));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (width, height) = (finalSize.Width, finalSize.Height);
        if (IsCollapsed)
        {
            ArrangeCollapsed(width, height);
        }
        else
        {
            ArrangeUncollapsed(width, height);
        }

        return finalSize;
    }

    // allocate_uncollapsed.
    private void ArrangeUncollapsed(double width, double height)
    {
        var sidebarWidth = Math.Min(SidebarWidth(width, false), width);
        var offset = Math.Floor(sidebarWidth * _progress);
        _sidebarWidth = sidebarWidth;
        if (offset > sidebarWidth)
        {
            (sidebarWidth, offset) = (offset, _sidebarWidth);
        }

        if (AtStart)
        {
            ArrangeSidebar(new Rect(offset - _sidebarWidth, 0, sidebarWidth, height));
            _contentBin.Arrange(new Rect(offset, 0, width - offset, height));
        }
        else
        {
            ArrangeSidebar(new Rect(sidebarWidth > _sidebarWidth ? width - sidebarWidth : width - offset, 0, sidebarWidth, height));
            _contentBin.Arrange(new Rect(0, 0, width - offset, height));
        }

        _shield.Arrange(default);
    }

    // allocate_collapsed.
    private void ArrangeCollapsed(double width, double height)
    {
        var sidebarWidth = SidebarWidth(width, true);
        _sidebarWidth = sidebarWidth;
        var offset = Math.Floor(sidebarWidth * _progress);
        double position;
        if (!AtStart)
        {
            position = width - offset;
            sidebarWidth = Math.Max(sidebarWidth, offset);
        }
        else
        {
            position = offset - sidebarWidth;
            if (offset > sidebarWidth)
            {
                position = 0;
                sidebarWidth = offset;
            }
        }

        _contentBin.Arrange(new Rect(0, 0, width, height));
        ArrangeSidebar(new Rect(position, 0, sidebarWidth, height));
        _shield.Arrange(new Rect(0, 0, width, height));
        var shadowProgress = 1 - Math.Min(_progress, 1);
        if (AtStart)
        {
            _shadow.Arrange(width, height, offset, 0, shadowProgress, ShadowDirection.Left);
        }
        else
        {
            _shadow.Arrange(width, height, -offset, 0, shadowProgress, ShadowDirection.Right);
        }
    }

    // allocate_sidebar: a bin wider than the sidebar, as a spring overshoots, keeps the sidebar against the outer edge.
    private void ArrangeSidebar(Rect rect)
    {
        _sidebarBin.Arrange(rect);
        if (_sidebarPresenter.Child is { } && rect.Width > _sidebarWidth)
        {
            _sidebarPresenter.Margin = AtStart ? new Thickness(rect.Width - _sidebarWidth, 0, 0, 0) : new Thickness(0, 0, rect.Width - _sidebarWidth, 0);
        }
        else
        {
            _sidebarPresenter.Margin = default;
        }
    }
}
