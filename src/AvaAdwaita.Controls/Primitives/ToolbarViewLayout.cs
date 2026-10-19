using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A toolbar view's content and bars, laid out as adw_toolbar_view_measure and adw_toolbar_view_size_allocate: the
/// content first, as AdwToolbarView's first child, then the top and bottom bars.
/// </summary>
public class ToolbarViewLayout : Panel
{
    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<ToolbarViewLayout, Control?>(nameof(Content));

    /// <summary>Defines <see cref="TopBar"/>.</summary>
    public static readonly StyledProperty<Control?> TopBarProperty =
        AvaloniaProperty.Register<ToolbarViewLayout, Control?>(nameof(TopBar));

    /// <summary>Defines <see cref="BottomBar"/>.</summary>
    public static readonly StyledProperty<Control?> BottomBarProperty =
        AvaloniaProperty.Register<ToolbarViewLayout, Control?>(nameof(BottomBar));

    /// <summary>The content.</summary>
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The top bars' revealer.</summary>
    public Control? TopBar
    {
        get => GetValue(TopBarProperty);
        set => SetValue(TopBarProperty, value);
    }

    /// <summary>The bottom bars' revealer.</summary>
    public Control? BottomBar
    {
        get => GetValue(BottomBarProperty);
        set => SetValue(BottomBarProperty, value);
    }

    private ToolbarView? View => TemplatedParent as ToolbarView;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var view = View;
        var extendTop = view?.ExtendContentToTopEdge == true;
        var extendBottom = view?.ExtendContentToBottomEdge == true;
        TopBar?.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        BottomBar?.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        var top = TopBar?.DesiredSize ?? default;
        var bottom = BottomBar?.DesiredSize ?? default;
        var contentHeight = availableSize.Height;
        if (!double.IsInfinity(contentHeight))
        {
            contentHeight = Math.Max(0, contentHeight - (extendTop ? 0 : top.Height) - (extendBottom ? 0 : bottom.Height));
        }

        Content?.Measure(new Size(availableSize.Width, contentHeight));
        var content = Content?.DesiredSize ?? default;
        var width = Math.Max(content.Width, Math.Max(top.Width, bottom.Width));
        var height = (extendTop, extendBottom) switch
        {
            (true, true) => Math.Max(content.Height, top.Height + bottom.Height),
            (true, false) => Math.Max(content.Height, top.Height) + bottom.Height,
            (false, true) => Math.Max(content.Height, bottom.Height) + top.Height,
            _ => content.Height + top.Height + bottom.Height,
        };
        return new Size(width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var view = View;
        var extendTop = view?.ExtendContentToTopEdge == true;
        var extendBottom = view?.ExtendContentToBottomEdge == true;
        var (width, height) = (finalSize.Width, finalSize.Height);
        var top = TopBar?.DesiredSize.Height ?? 0;
        var bottom = BottomBar?.DesiredSize.Height ?? 0;

        // Avalonia measures one size: the bars keep theirs, as their minimum and natural heights match.
        var contentMin = Math.Max(0, (Content?.DesiredSize.Height ?? 0) - (extendTop ? top : 0) - (extendBottom ? bottom : 0));
        var topHeight = Math.Clamp(height - contentMin - bottom, top, top);
        var bottomHeight = Math.Clamp(height - contentMin - topHeight, bottom, bottom);
        var contentHeight = height - (extendTop ? 0 : topHeight) - (extendBottom ? 0 : bottomHeight);
        var contentOffset = extendTop ? 0 : topHeight;

        TopBar?.Arrange(new Rect(0, 0, width, topHeight));
        BottomBar?.Arrange(new Rect(0, height - bottomHeight, width, bottomHeight));
        Content?.Arrange(new Rect(0, contentOffset, width, Math.Max(0, contentHeight)));
        if (view is not null)
        {
            view.TopBarHeight = topHeight;
            view.BottomBarHeight = bottomHeight;
        }

        return finalSize;
    }
}
