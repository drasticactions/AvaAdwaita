using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// The editable area of an entry row (adw-entry-row.c's measure_editable_area and allocate_editable_area): the
/// title over the text, which shows as a larger dimmed title while the row is empty, and the edit icon, indicator or
/// apply button at the end. <see cref="EmptyProgress"/> moves between the empty look (0) and the editing look (1).
/// </summary>
public class EntryRowArea : Panel
{
    /// <summary>GTK's TITLE_SPACING: between the title and the text.</summary>
    public const double TitleSpacing = 3;

    /// <summary>Defines <see cref="EmptyProgress"/>.</summary>
    public static readonly StyledProperty<double> EmptyProgressProperty =
        AvaloniaProperty.Register<EntryRowArea, double>(nameof(EmptyProgress));

    /// <summary>Defines <see cref="EmptyTitle"/>.</summary>
    public static readonly StyledProperty<Control?> EmptyTitleProperty =
        AvaloniaProperty.Register<EntryRowArea, Control?>(nameof(EmptyTitle));

    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<Control?> TitleProperty =
        AvaloniaProperty.Register<EntryRowArea, Control?>(nameof(Title));

    /// <summary>Defines <see cref="Text"/>.</summary>
    public static readonly StyledProperty<TextBox?> TextProperty =
        AvaloniaProperty.Register<EntryRowArea, TextBox?>(nameof(Text));

    static EntryRowArea()
    {
        AffectsArrange<EntryRowArea>(EmptyProgressProperty);
    }

    /// <summary>0 shows the empty look: the large title alone. 1 shows the small title over the text.</summary>
    public double EmptyProgress
    {
        get => GetValue(EmptyProgressProperty);
        set => SetValue(EmptyProgressProperty, value);
    }

    /// <summary>The title as it shows while the row is empty.</summary>
    public Control? EmptyTitle
    {
        get => GetValue(EmptyTitleProperty);
        set => SetValue(EmptyTitleProperty, value);
    }

    /// <summary>The title as it shows over the text.</summary>
    public Control? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The text.</summary>
    public TextBox? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private bool IsIcon(Control c) => c != EmptyTitle && c != Title && c != Text;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        double textW = 0, textH = 0, titleW = 0, titleH = 0, emptyW = 0, emptyH = 0, iconW = 0, iconH = 0;
        foreach (var c in Children)
        {
            c.Measure(availableSize);
            if (!c.IsVisible)
            {
                continue;
            }

            var d = c.DesiredSize;
            if (c == Text)
            {
                (textW, textH) = (d.Width, TextHeight(Text));
            }
            else if (c == Title)
            {
                (titleW, titleH) = (d.Width, d.Height);
            }
            else if (c == EmptyTitle)
            {
                (emptyW, emptyH) = (d.Width, d.Height);
            }
            else
            {
                iconW = Math.Max(iconW, d.Width);
                iconH = Math.Max(iconH, d.Height);
            }
        }

        return new Size(
            Math.Max(textW, Math.Max(titleW, emptyW)) + iconW,
            Math.Max(textH + TitleSpacing + titleH, Math.Max(emptyH, iconH)));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (width, height) = (finalSize.Width, finalSize.Height);
        var iconWidth = 0.0;
        foreach (var c in Children)
        {
            if (c.IsVisible && IsIcon(c))
            {
                iconWidth = Math.Max(iconWidth, c.DesiredSize.Width);
            }
        }

        iconWidth = Math.Min(iconWidth, width);
        var p = EmptyProgress;
        var titleHeight = Title?.DesiredSize.Height ?? 0;
        var emptyHeight = EmptyTitle?.DesiredSize.Height ?? 0;
        var textHeight = Text is { } tb ? TextHeight(tb) : 0;
        var emptyScale = titleHeight > 0 && emptyHeight > 0 ? Lerp(1, titleHeight / emptyHeight, p) : 1;
        var titleScale = titleHeight > 0 && emptyHeight > 0 ? Lerp(emptyHeight / titleHeight, 1, p) : 1;
        var titleOffset = Lerp((height - emptyHeight) / 2, (height - titleHeight - textHeight - TitleSpacing) / 2, p);

        Place(EmptyTitle, emptyHeight, emptyScale, titleOffset, width - iconWidth, 1 - p);
        Place(Title, titleHeight, titleScale, titleOffset, width - iconWidth, p);
        if (Text is { } text)
        {
            // The text sits under the title: its line is centered in what the title leaves.
            text.Padding = new Thickness(0, titleHeight + TitleSpacing, 0, 0);
            text.Opacity = p;
            text.Arrange(new Rect(0, 0, Math.Max(0, width - iconWidth), height));
        }

        foreach (var c in Children)
        {
            if (IsIcon(c))
            {
                c.Arrange(new Rect(width - iconWidth, 0, iconWidth, height));
            }
        }

        return finalSize;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    // The text's line height: what the text box measures without the padding that places it.
    private static double TextHeight(TextBox text) => Math.Max(0, text.DesiredSize.Height - text.Padding.Top - text.Padding.Bottom);

    private static void Place(Control? label, double height, double scale, double offset, double width, double opacity)
    {
        if (label is null)
        {
            return;
        }

        label.HorizontalAlignment = HorizontalAlignment.Left;
        label.RenderTransformOrigin = RelativePoint.TopLeft;
        label.RenderTransform = new Avalonia.Media.MatrixTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(0, offset));
        label.Opacity = opacity;
        label.Arrange(new Rect(0, 0, Math.Max(0, width), height));
    }
}
