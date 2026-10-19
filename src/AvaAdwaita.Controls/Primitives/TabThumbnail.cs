using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using AvaAdwaita.Animation;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab overview's thumbnail of a page (AdwTabThumbnail): the page's picture in a card with a close button over its
/// corner, and the page's icon and title under it. A pinned page's card holds its icon and title instead, with an unpin
/// icon. A dot marks a page needing attention.
/// </summary>
public class TabThumbnail : AdwBox
{
    private const double FadeTransitionDuration = 250;
    private const double PinnedMargin = 10;

    private readonly AdwBox _thumbOverlay = Node("overlay", "thumbnail");
    private readonly AdwBox _card = Node("overlay", "card");
    private readonly Button _close = new() { Focusable = false, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
    private readonly AdwIcon _unpin = new() { IconName = "adw-tab-unpin-symbolic", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
    private readonly Button _indicator = new() { Focusable = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, IsVisible = false };
    private readonly AdwIcon _indicatorIcon = new();
    private readonly Revealer _attention = new() { Transition = RevealerTransition.Crossfade, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false, Focusable = false };
    private readonly AdwBox _iconTitleBox = Node("box", "horizontal icon-title-box");
    private readonly AdwIcon _icon = new() { IsVisible = false };
    private readonly FadingLabel _title = new();
    private readonly PinnedBox? _pinnedBox;
    private readonly TimedAnimation _fade;
    private TabPage? _page;

    /// <summary>Creates a thumbnail for <paramref name="view"/>'s pages, pinned or not.</summary>
    public TabThumbnail(TabView view, bool pinned)
    {
        View = view;
        IsPinned = pinned;
        Css.SetNode(this, "tabthumbnail");
        Picture = new TabPicture(view) { VerticalAlignment = VerticalAlignment.Stretch };
        var closeIcon = new AdwIcon { IconName = "window-close-symbolic" };
        Css.SetNode(closeIcon, "image");
        _close.Content = closeIcon;
        _close.Classes.AddRange(["tab-close-button"]);
        Css.SetClasses(_close, "right top");
        _close[!ToolTip.TipProperty] = _close.GetResourceObservable("AdwStringCloseTab").ToBinding();
        _close.Click += (_, _) =>
        {
            if (_page is { } page)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => View.ClosePage(page));
            }
        };
        Css.SetNode(_unpin, "image");
        Css.SetClasses(_unpin, "right top tab-unpin-icon");
        _unpin.PointerReleased += (_, e) =>
        {
            if (_page is { } page && e.InitialPressMouseButton == MouseButton.Left)
            {
                e.Handled = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => View.SetPagePinned(page, false));
            }
        };
        Css.SetNode(_indicatorIcon, "image");
        _indicator.Content = _indicatorIcon;
        _indicator.Classes.AddRange(["tab-indicator", "image-button"]);
        Css.SetClasses(_indicator, "left top");
        _indicator.Click += (_, _) =>
        {
            if (_page is { } page)
            {
                View.ActivateIndicator(page);
            }
        };
        var dot = new AdwBox();
        Css.SetNode(dot, "widget");
        _attention.Child = dot;
        Css.SetClasses(_attention, "right top needs-attention");
        Css.SetNode(_icon, "image");
        Css.SetClasses(_icon, "tab-icon");
        _iconTitleBox.Child = new AdwStack { Orientation = Orientation.Horizontal, Children = { _icon, _title } };
        _iconTitleBox.HorizontalAlignment = HorizontalAlignment.Center;
        _iconTitleBox.IsHitTestVisible = false;

        var cardLayers = new OverlayPanel { Children = { Picture, _close, _unpin, _indicator } };
        _card.Child = cardLayers;
        _thumbOverlay.Child = new OverlayPanel { Children = { _card, _attention } };
        var contents = new AdwStack { Orientation = Orientation.Vertical, BoxSpacing = 6, Children = { _thumbOverlay } };
        if (pinned)
        {
            // adw_tab_thumbnail_constructed: the icon and title go in the card.
            Css.SetClasses(this, "pinned");
            Picture.IsVisible = false;
            _close.IsVisible = false;
            _pinnedBox = new PinnedBox(this) { IsHitTestVisible = false };
            Css.SetNode(_pinnedBox, "widget");
            Css.SetClasses(_pinnedBox, "pinned-box");
            _iconTitleBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _pinnedBox.Child = _iconTitleBox;
            cardLayers.Children.Add(_pinnedBox);
        }
        else
        {
            _unpin.IsVisible = false;
            contents.Children.Add(_iconTitleBox);
        }

        var box = Node("box", "vertical");
        box.Child = contents;
        Child = box;
        _fade = new TimedAnimation(this, FadeTransitionDuration, AdwEasing.Ease, v =>
        {
            if (IsPinned)
            {
                _unpin.Opacity = v;
                _iconTitleBox.Opacity = v;
            }
            else
            {
                _close.Opacity = v;
            }

            _indicator.Opacity = v;
            _attention.Opacity = v;
        });
        _fade.Jump(1);
    }

    /// <summary>The view whose page the thumbnail shows.</summary>
    public TabView View { get; }

    /// <summary>Whether the thumbnail is a pinned one.</summary>
    public bool IsPinned { get; }

    /// <summary>The page's picture.</summary>
    public TabPicture Picture { get; }

    /// <summary>The page the thumbnail shows.</summary>
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

            Update();
        }
    }

    /// <summary>Whether the unpin icon and indicator swap sides.</summary>
    internal bool IsInverted { get; set; }

    internal FadingLabel TitleLabel => _title;

    internal AdwBox IconTitleBox => _iconTitleBox;

    internal AdwIcon UnpinIcon => _unpin;

    internal Button IndicatorButton => _indicator;

    /// <summary>Fades the buttons out, as the overview's transition takes the thumbnail's place.</summary>
    internal void FadeOut() => _fade.Jump(0);

    /// <summary>Fades the buttons back in.</summary>
    internal void FadeIn() => _fade.Animate(1);

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        OverflowClip.Apply(_card, (Control)_card.Child!);
        return size;
    }

    private static AdwBox Node(string name, string classes)
    {
        var box = new AdwBox();
        Css.SetNode(box, name);
        Css.SetClasses(box, classes);
        return box;
    }

    private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Update();

    // update_tooltip, update_icon, update_indicator and the attention dot.
    private void Update()
    {
        var page = _page;
        _title.Label.Text = page?.Title;
        ToolTip.SetTip(this, page?.Tooltip is { Length: > 0 } tooltip ? tooltip : page?.Title);
        _icon.IconName = page?.IconName;
        _icon.IsVisible = page?.IconName is not null || page?.IsLoading == true;
        _indicatorIcon.IconName = page?.IndicatorIconName;
        _indicator.IsVisible = page?.IndicatorIconName is not null;
        _indicator.IsHitTestVisible = page?.IsIndicatorActivatable == true;
        ToolTip.SetTip(_indicator, page?.IndicatorTooltip);
        _attention.IsRevealed = page?.NeedsAttention == true;
        Css.SetClasses(this, (IsPinned ? "pinned " : string.Empty) + (page?.IsLoading == true ? "loading " : string.Empty) + (page?.IndicatorIconName is not null ? "indicator" : string.Empty));
        _pinnedBox?.InvalidateMeasure();
    }

    // measure_pinned_tab and allocate_pinned_tab: the icon and title centered between the unpin icon and the indicator.
    private sealed class PinnedBox(TabThumbnail thumbnail) : AdwBox
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var box = thumbnail._iconTitleBox;
            box.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            var buttons = thumbnail._indicator.IsVisible ? thumbnail._unpin.DesiredSize.Width + thumbnail._indicator.DesiredSize.Width : 0;
            return new Size(box.DesiredSize.Width + buttons, box.DesiredSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var (left, right) = (thumbnail._indicator.IsVisible ? thumbnail._indicator.DesiredSize.Width : 0, thumbnail._unpin.IsVisible ? thumbnail._unpin.DesiredSize.Width : 0);
            if ((FlowDirection == Avalonia.Media.FlowDirection.RightToLeft) != thumbnail.IsInverted)
            {
                (left, right) = (right, left);
            }

            left = Math.Max(left, PinnedMargin);
            right = Math.Max(right, PinnedMargin);
            var box = thumbnail._iconTitleBox;
            var width = Math.Min(finalSize.Width - right - left, box.DesiredSize.Width);
            var x = Math.Floor((finalSize.Width - width) / 2);
            if (x + width > finalSize.Width - right)
            {
                x = finalSize.Width - right - width;
            }

            x = Math.Max(x, left);
            box.Arrange(new Rect(x, 0, Math.Max(0, width), finalSize.Height));
            return finalSize;
        }
    }
}
