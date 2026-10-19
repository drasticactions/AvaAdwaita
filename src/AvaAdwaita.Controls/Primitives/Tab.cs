using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaAdwaita.Animation;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab of a <see cref="TabBar"/> (AdwTab): a <c>tab</c> node with the page's icon and title centered, an indicator
/// button at the start, a close button at the end that shows on hover or while selected, and a mark when the page needs
/// attention.
/// </summary>
public class Tab : AdwBox
{
    private const double BaseWidth = 118;
    private const double BaseWidthPinned = 26;
    private const double AttentionIndicatorPinnedWidth = 14;
    private const double AttentionIndicatorWidthMultiplier = 0.6;
    private const double AttentionIndicatorMinWidth = 20;
    private const double AttentionIndicatorMaxWidth = 180;
    private const double CloseButtonAnimationDuration = 150;
    private const double AttentionIndicatorAnimationDuration = 250;

    private readonly TabLayout _layout;
    private readonly AdwIcon _icon = new() { Margin = new Thickness(4, 0, 2, 0) };
    private readonly AdwSpinner _spinner = new() { Margin = new Thickness(4, 0, 2, 0), IsVisible = false };
    private readonly FadingLabel _title = new() { Margin = new Thickness(4, 0) };
    private readonly Button _indicator = new() { Focusable = false, VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
    private readonly AdwIcon _indicatorIcon = new();
    private readonly Button _close = new() { Focusable = false, VerticalAlignment = VerticalAlignment.Center, Opacity = 0, IsHitTestVisible = false };
    private readonly AdwBox _attention = new() { VerticalAlignment = VerticalAlignment.Bottom };
    private readonly TimedAnimation _closeAnimation;
    private readonly TimedAnimation _attentionAnimation;
    private TabPage? _page;
    private bool _hovering;
    private bool _showClose;
    private bool _fullyVisible = true;
    private bool _dragging;
    private bool _inverted;

    /// <summary>Creates a tab for <paramref name="view"/>'s pages, pinned or not.</summary>
    public Tab(TabView view, bool pinned)
    {
        View = view;
        IsPinned = pinned;
        Css.SetNode(this, "tab");
        Focusable = true;
        Css.SetNode(_icon, "image");
        Css.SetNode(_spinner, "image");
        Css.SetClasses(_title, "tab-title");
        Css.SetNode(_indicatorIcon, "image");
        _indicator.Content = _indicatorIcon;
        _indicator.Classes.AddRange(["flat", "tab-indicator"]);
        _indicator.Click += (_, _) =>
        {
            if (_page is { } page)
            {
                View.ActivateIndicator(page);
            }
        };
        var closeIcon = new AdwIcon { IconName = "window-close-symbolic" };
        Css.SetNode(closeIcon, "image");
        _close.Content = closeIcon;
        _close.Classes.AddRange(["flat", "tab-close-button"]);
        _close[!ToolTip.TipProperty] = _close.GetResourceObservable("AdwStringCloseTab").ToBinding();
        _close.Click += (_, _) =>
        {
            if (_page is { } page)
            {
                // close_clicked_cb: once the click is over.
                Avalonia.Threading.Dispatcher.UIThread.Post(() => CloseClicked?.Invoke(this, page));
            }
        };
        Css.SetNode(_attention, "indicator");
        _layout = new TabLayout(this, _icon, _spinner, _title, _indicator, _close, _attention);
        Child = _layout;
        if (pinned)
        {
            Css.SetClasses(this, "pinned");
            _title.IsVisible = false;
            _close.IsVisible = false;
            _icon.Margin = _spinner.Margin = default;
        }

        _closeAnimation = new TimedAnimation(this, CloseButtonAnimationDuration, AdwEasing.EaseInOut, v =>
        {
            _close.Opacity = v;
            _close.IsHitTestVisible = v > 0;
            _layout.InvalidateVisual();
        });
        _attentionAnimation = new TimedAnimation(this, AttentionIndicatorAnimationDuration, AdwEasing.EaseInOut, _ => _layout.InvalidateArrange());
        view.PropertyChanged += (_, e) =>
        {
            if (e.Property == TabView.DefaultIconNameProperty)
            {
                UpdateIcons();
            }
        };
    }

    /// <summary>Raised when the close button is clicked, with the page.</summary>
    public event EventHandler<TabPage>? CloseClicked;

    /// <summary>Raised when the tab's hover, press or selection changes, for the separators beside it.</summary>
    public event EventHandler? StateChanged;

    /// <summary>The view whose page the tab shows.</summary>
    public TabView View { get; }

    /// <summary>Whether the tab is a pinned one: its icon alone.</summary>
    public bool IsPinned { get; }

    /// <summary>The page the tab shows.</summary>
    public TabPage? Page
    {
        get => _page;
        set
        {
            if (_page is not null)
            {
                _page.PropertyChanged -= OnPagePropertyChanged;
            }

            _page = value;
            if (value is not null)
            {
                value.PropertyChanged += OnPagePropertyChanged;
            }

            UpdateTitle();
            UpdateIcons();
            ToolTip.SetTip(_indicator, value?.IndicatorTooltip);
            UpdateSelected();
            _attentionAnimation.Jump(value?.NeedsAttention == true ? 1 : 0);
        }
    }

    /// <summary>Whether the close button is at the start and the indicator at the end.</summary>
    public bool IsInverted
    {
        get => _inverted;
        set
        {
            _inverted = value;
            _layout.InvalidateArrange();
        }
    }

    /// <summary>Whether the tab is being dragged, which shows it selected.</summary>
    public bool IsDragging
    {
        get => _dragging;
        set
        {
            _dragging = value;
            UpdateSelected();
        }
    }

    /// <summary>Whether the whole tab is in view, which lets hovering show its close button.</summary>
    public bool IsFullyVisible
    {
        get => _fullyVisible;
        set
        {
            if (_fullyVisible == value)
            {
                return;
            }

            _fullyVisible = value;
            UpdateState();
            UpdateIndicator();
        }
    }

    /// <summary>Whether the tab shows its page selected.</summary>
    internal bool IsSelected { get; private set; }

    /// <summary>Whether the close button covers the title's end, which fades out under it.</summary>
    internal bool CloseOverlap { get; set; }

    internal bool IsTitleInverted { get; private set; }

    internal double AttentionWidth(double centerWidth)
    {
        // get_attention_indicator_width.
        var baseWidth = IsPinned ? AttentionIndicatorPinnedWidth
            : Math.Clamp(centerWidth * AttentionIndicatorWidthMultiplier, AttentionIndicatorMinWidth, AttentionIndicatorMaxWidth);
        return Math.Truncate(baseWidth * _attentionAnimation.Value);
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (e.Pointer.Type != PointerType.Touch)
        {
            _hovering = true;
            UpdateState();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovering = false;
        UpdateState();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // activate_cb: Space and Enter go into the page.
        if (!e.Handled && e.Key is Key.Space or Key.Enter && _page?.Content is { } content)
        {
            e.Handled = true;
            content.Focus();
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FlowDirectionProperty)
        {
            UpdateTitle();
        }
    }

    private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TabPage.TitleProperty || e.Property == TabPage.TooltipProperty)
        {
            UpdateTitle();
        }
        else if (e.Property == TabPage.IconNameProperty || e.Property == TabPage.IsLoadingProperty || e.Property == TabPage.IndicatorIconNameProperty)
        {
            UpdateIcons();
        }
        else if (e.Property == TabPage.IndicatorTooltipProperty)
        {
            ToolTip.SetTip(_indicator, _page?.IndicatorTooltip);
        }
        else if (e.Property == TabPage.IsIndicatorActivatableProperty)
        {
            UpdateIndicator();
        }
        else if (e.Property == TabPage.NeedsAttentionProperty)
        {
            UpdateNeedsAttention();
        }
        else if (e.Property == TabPage.IsSelectedProperty)
        {
            UpdateSelected();
        }
    }

    // update_state: selected while its page is or while dragged; the close button shows on hover, selected or dragged.
    private void UpdateState()
    {
        PseudoClasses.Set(":selected", IsSelected);
        var showClose = (_hovering && _fullyVisible) || IsSelected || _dragging;
        if (_showClose != showClose)
        {
            _showClose = showClose;
            _closeAnimation.Animate(showClose ? 1 : 0);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSelected()
    {
        IsSelected = _dragging || _page?.IsSelected == true;
        UpdateState();
        UpdateIndicator();
    }

    // update_title: the title, and whether it reads the other way to the tab.
    private void UpdateTitle()
    {
        var title = _page?.Title;
        _title.Label.Text = title;
        var rtlTitle = title is { Length: > 0 } && IsRtlText(title);
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var inverted = title is { Length: > 0 } && rtlTitle != rtl;
        if (inverted != IsTitleInverted)
        {
            IsTitleInverted = inverted;
            _layout.InvalidateArrange();
        }

        ToolTip.SetTip(this, _page?.Tooltip is { Length: > 0 } tooltip ? tooltip : title);
    }

    // adw_find_base_dir: the direction of the first strong letter.
    private static bool IsRtlText(string text)
    {
        foreach (var c in text)
        {
            if (c is >= '\u0590' and <= '\u08FF' or >= '\uFB1D' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF')
            {
                return true;
            }

            if (char.IsLetter(c))
            {
                return false;
            }
        }

        return false;
    }

    // update_icons: a pinned tab without an icon shows the view's default one; an indicator replaces a pinned tab's icon.
    private void UpdateIcons()
    {
        var icon = _page?.IconName;
        var loading = _page?.IsLoading == true;
        var indicator = _page?.IndicatorIconName;
        if (IsPinned && icon is null)
        {
            icon = View.DefaultIconName;
        }

        _icon.IconName = icon;
        var showIcon = (icon is not null || loading) && (!IsPinned || indicator is null);
        _icon.IsVisible = showIcon && !loading;
        _spinner.IsVisible = showIcon && loading;
        _indicatorIcon.IconName = indicator;
        _indicator.IsVisible = indicator is not null;
        Css.SetClasses(this, (IsPinned ? "pinned " : string.Empty) + (loading ? "loading " : string.Empty) + (_page?.NeedsAttention == true ? "needs-attention" : string.Empty));
        _layout.InvalidateMeasure();
    }

    // update_indicator: the indicator takes clicks on the selected tab, or a fully visible unpinned one.
    private void UpdateIndicator()
    {
        var activatable = _page?.IsIndicatorActivatable == true;
        _indicator.IsHitTestVisible = activatable && (IsSelected || (!IsPinned && _fullyVisible));
    }

    private void UpdateNeedsAttention()
    {
        _attentionAnimation.Animate(_page?.NeedsAttention == true ? 1 : 0);
        UpdateIcons();
    }
}
