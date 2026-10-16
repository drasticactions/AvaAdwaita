using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Docks its last child to <see cref="Dock"/> and fills the rest with the first. A dock panel needs the docked
/// child first; GTK's node order sometimes puts it last, as a notebook's header when the tabs are at the bottom.
/// </summary>
public class AdwDockLast : Panel
{
    /// <summary>Defines <see cref="Dock"/>.</summary>
    public static readonly StyledProperty<Dock> DockProperty =
        AvaloniaProperty.Register<AdwDockLast, Dock>(nameof(Dock), Avalonia.Controls.Dock.Bottom);

    static AdwDockLast()
    {
        AffectsMeasure<AdwDockLast>(DockProperty);
    }

    /// <summary>Gets or sets the side the last child docks to.</summary>
    public Dock Dock
    {
        get => GetValue(DockProperty);
        set => SetValue(DockProperty, value);
    }

    private bool Horizontal => Dock is Dock.Left or Dock.Right;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 2)
        {
            return base.MeasureOverride(availableSize);
        }

        var fill = Children[0];
        var docked = Children[Children.Count - 1];
        docked.Measure(availableSize);
        var d = docked.DesiredSize;
        if (Horizontal)
        {
            fill.Measure(new Size(Math.Max(0, availableSize.Width - d.Width), availableSize.Height));
            return new Size(d.Width + fill.DesiredSize.Width, Math.Max(d.Height, fill.DesiredSize.Height));
        }

        fill.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - d.Height)));
        return new Size(Math.Max(d.Width, fill.DesiredSize.Width), d.Height + fill.DesiredSize.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 2)
        {
            return base.ArrangeOverride(finalSize);
        }

        var fill = Children[0];
        var docked = Children[Children.Count - 1];
        var d = docked.DesiredSize;
        switch (Dock)
        {
            case Dock.Bottom:
                docked.Arrange(new Rect(0, finalSize.Height - d.Height, finalSize.Width, d.Height));
                fill.Arrange(new Rect(0, 0, finalSize.Width, Math.Max(0, finalSize.Height - d.Height)));
                break;
            case Dock.Top:
                docked.Arrange(new Rect(0, 0, finalSize.Width, d.Height));
                fill.Arrange(new Rect(0, d.Height, finalSize.Width, Math.Max(0, finalSize.Height - d.Height)));
                break;
            case Dock.Right:
                docked.Arrange(new Rect(finalSize.Width - d.Width, 0, d.Width, finalSize.Height));
                fill.Arrange(new Rect(0, 0, Math.Max(0, finalSize.Width - d.Width), finalSize.Height));
                break;
            default:
                docked.Arrange(new Rect(0, 0, d.Width, finalSize.Height));
                fill.Arrange(new Rect(d.Width, 0, Math.Max(0, finalSize.Width - d.Width), finalSize.Height));
                break;
        }

        return finalSize;
    }
}
