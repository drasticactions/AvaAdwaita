using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Sizes a single-line text box's text as GtkText does (gtk_text_measure): its natural width is 150 px or its
/// placeholder's, whichever is wider, however long the text, which scrolls. It takes less when less is offered, as
/// GtkText's minimum width is none.
/// </summary>
public class AdwEntryWidth : Decorator
{
    /// <summary>GtkText's NAT_ENTRY_WIDTH.</summary>
    public const double NaturalWidth = 150;

    /// <summary>Defines <see cref="Placeholder"/>.</summary>
    public static readonly StyledProperty<Control?> PlaceholderProperty =
        AvaloniaProperty.Register<AdwEntryWidth, Control?>(nameof(Placeholder));

    /// <summary>The placeholder label, whose natural width the text's is at least.</summary>
    public Control? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        child.Measure(availableSize);
        var placeholder = Placeholder is { IsVisible: true } p ? p.DesiredSize.Width : 0;
        var width = Math.Min(availableSize.Width, Math.Max(NaturalWidth, placeholder));
        return new Size(width, child.DesiredSize.Height);
    }
}
