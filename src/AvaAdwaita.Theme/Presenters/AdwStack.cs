using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A stack panel whose spacing is the nearest node's <c>border-spacing</c> plus <see cref="BoxSpacing"/>, as a GTK box
/// lays out its children (gtk_box_layout's get_spacing).
/// Space beyond the children's sizes goes to the children marked <see cref="ExpandProperty"/>, as GTK's
/// <c>hexpand</c> and <c>vexpand</c>.
/// </summary>
public class AdwStack : StackPanel
{
    /// <summary>The child takes a share of the space left over along the stack.</summary>
    public static readonly AttachedProperty<bool> ExpandProperty =
        AvaloniaProperty.RegisterAttached<AdwStack, Control, bool>("Expand");

    /// <summary>Defines <see cref="BoxSpacing"/>.</summary>
    public static readonly StyledProperty<double> BoxSpacingProperty =
        AvaloniaProperty.Register<AdwStack, double>(nameof(BoxSpacing));

    static AdwStack()
    {
        AffectsParentArrange<AdwStack>(ExpandProperty);
        Nodes.Css.StyleProperty.Changed.AddClassHandler<AdwStack>((s, _) => s.UpdateSpacing());
        BoxSpacingProperty.Changed.AddClassHandler<AdwStack>((s, _) => s.UpdateSpacing());
    }

    /// <summary>The box's own spacing, GtkBox:spacing, which GTK adds to the CSS border-spacing.</summary>
    public double BoxSpacing
    {
        get => GetValue(BoxSpacingProperty);
        set => SetValue(BoxSpacingProperty, value);
    }

    private void UpdateSpacing()
    {
        var css = GetValue(Nodes.Css.StyleProperty) is { } style
            ? Orientation == Orientation.Horizontal ? style.Size.BorderSpacing.X.Value : style.Size.BorderSpacing.Y.Value
            : 0;
        Spacing = css + BoxSpacing;
    }

    /// <summary>Gets <see cref="ExpandProperty"/>.</summary>
    public static bool GetExpand(Control c) => c.GetValue(ExpandProperty);

    /// <summary>Sets <see cref="ExpandProperty"/>.</summary>
    public static void SetExpand(Control c, bool v) => c.SetValue(ExpandProperty, v);

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        var expanding = visible.Count(GetExpand);
        if (expanding == 0)
        {
            return base.ArrangeOverride(finalSize);
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var used = visible.Sum(c => horizontal ? c.DesiredSize.Width : c.DesiredSize.Height) + (Spacing * Math.Max(0, visible.Count - 1));
        var extra = Math.Max(0, (horizontal ? finalSize.Width : finalSize.Height) - used);
        var share = Math.Floor(extra / expanding);
        var remainder = extra - (share * expanding);
        var at = 0.0;
        foreach (var c in visible)
        {
            var length = horizontal ? c.DesiredSize.Width : c.DesiredSize.Height;
            if (GetExpand(c))
            {
                length += share;
                if (remainder > 0)
                {
                    length += remainder;
                    remainder = 0;
                }
            }

            c.Arrange(horizontal ? new Rect(at, 0, length, finalSize.Height) : new Rect(0, at, finalSize.Width, length));
            at += length + Spacing;
        }

        return finalSize;
    }
}
