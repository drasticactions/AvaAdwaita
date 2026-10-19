using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// An alert dialog's heading (adw-alert-dialog.c's heading_bin): the heading in <c>title-2</c> on one line while it
/// fits, else in <c>title-4</c>, wrapped.
/// </summary>
public class AlertHeading : Control
{
    private readonly AdwLabel _large = new() { TextAlignment = Avalonia.Media.TextAlignment.Center };
    private readonly AdwLabel _small = new() { TextAlignment = Avalonia.Media.TextAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    /// <summary>Creates a heading: a <c>widget.heading-bin</c> node with the two labels.</summary>
    public AlertHeading()
    {
        Css.SetNode(this, "widget");
        Css.SetClasses(this, "heading-bin");
        Css.SetNode(_large, "label");
        Css.SetClasses(_large, "title-2");
        Css.SetNode(_small, "label");
        Css.SetClasses(_small, "title-4");
        VisualChildren.Add(_large);
        VisualChildren.Add(_small);
    }

    /// <summary>The heading.</summary>
    public string? Text
    {
        get => _large.Text;
        set
        {
            _large.Text = value;
            _small.Text = value;
            InvalidateMeasure();
        }
    }

    /// <summary>Uses the small heading always, as a short dialog does.</summary>
    public bool IsShort { get; set; }

    // The bin's padding, as message-area > .heading-bin sets it.
    private Thickness Inset => Css.GetStyle(this) is { } s ? new CssBoxGeometry(s, default) is var g
        ? new Thickness(g.Margin.Left + g.Border.Left + g.Padding.Left, g.Margin.Top + g.Border.Top + g.Padding.Top, g.Margin.Right + g.Border.Right + g.Padding.Right, g.Margin.Bottom + g.Border.Bottom + g.Padding.Bottom)
        : default : default;

    private double LargeWidth()
    {
        _large.Measure(Size.Infinity);
        return _large.DesiredSize.Width;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var inset = Inset;
        var available = availableSize.Deflate(inset);
        var small = IsShort || LargeWidth() > available.Width;
        _small.Measure(available);
        _large.Measure(Size.Infinity);
        Size size;
        if (double.IsInfinity(available.Width))
        {
            // measure_heading for no width: the larger of the two.
            size = new Size(Math.Max(_large.DesiredSize.Width, _small.DesiredSize.Width), Math.Max(_large.DesiredSize.Height, _small.DesiredSize.Height));
        }
        else
        {
            size = small ? _small.DesiredSize : _large.DesiredSize;
        }

        return size.Inflate(inset);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // allocate_heading: the large label when its natural width fits.
        var content = new Rect(finalSize).Deflate(Inset);
        var small = IsShort || LargeWidth() > content.Width;
        _large.IsVisible = !small;
        _small.IsVisible = small;
        (small ? _small : _large).Arrange(content);
        return finalSize;
    }
}
