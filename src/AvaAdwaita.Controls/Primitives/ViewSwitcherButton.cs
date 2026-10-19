using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A view switcher's button for one page (AdwViewSwitcherButton): a flat toggle with the page's icon and title, the
/// icon beside the title when horizontal and over it when vertical.
/// </summary>
public class ViewSwitcherButton : ToggleButton
{
    /// <summary>libadwaita's MIN_NAT_BUTTON_WIDTH_WIDE and MIN_NAT_BUTTON_WIDTH_NARROW.</summary>
    private const double WideNaturalWidth = 120;
    private const double NarrowNaturalWidth = 100;

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ViewSwitcherButton, Orientation>(nameof(Orientation));

    /// <summary>Defines <see cref="Page"/>.</summary>
    public static readonly StyledProperty<ViewStackPage?> PageProperty =
        AvaloniaProperty.Register<ViewSwitcherButton, ViewStackPage?>(nameof(Page));

    static ViewSwitcherButton()
    {
        AffectsMeasure<ViewSwitcherButton>(OrientationProperty);
        CssNodeProviders.Register<ViewSwitcherButton>(b => [b.Orientation == Orientation.Horizontal ? "horizontal" : "vertical"], OrientationProperty);
    }

    /// <summary>Creates a button: a flat toggle with none of GtkButton's automatic classes.</summary>
    public ViewSwitcherButton()
    {
        Classes.Add("flat");
        Classes.Add("adw-no-auto-class");
    }

    /// <summary>The icon beside the title (horizontal) or over it (vertical).</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>The page the button shows.</summary>
    public ViewStackPage? Page
    {
        get => GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // adw_view_switcher_button_measure: the natural width is at least 120 (wide) or 100 (narrow).
        var size = base.MeasureOverride(availableSize);
        var min = Orientation == Orientation.Horizontal ? WideNaturalWidth : NarrowNaturalWidth;
        return new Size(System.Math.Min(System.Math.Max(size.Width, min), availableSize.Width), size.Height);
    }
}
