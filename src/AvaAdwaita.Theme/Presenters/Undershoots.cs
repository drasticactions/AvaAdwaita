using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Shows a scroll viewer's <c>undershoot</c> nodes as GtkScrolledWindow draws them: a 40 px band along each edge
/// of the viewport past which there is more content. Mark each node's element with <see cref="EdgeProperty"/>.
/// </summary>
/// <remarks>Follows gtk_scrolled_window_snapshot_undershoot (gtkscrolledwindow.c).</remarks>
public static class Undershoots
{
    /// <summary>The edge an undershoot element stands for.</summary>
    public static readonly AttachedProperty<Dock?> EdgeProperty =
        AvaloniaProperty.RegisterAttached<Control, Dock?>("Edge", typeof(Undershoots));

    private static readonly AttachedProperty<EventHandler<ScrollChangedEventArgs>?> HandlerProperty =
        AvaloniaProperty.RegisterAttached<Control, EventHandler<ScrollChangedEventArgs>?>("Handler", typeof(Undershoots));

    private const double Size = 40;

    static Undershoots()
    {
        EdgeProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            c.AttachedToVisualTree -= OnAttached;
            c.DetachedFromVisualTree -= OnDetached;
            if (e.NewValue is Dock edge)
            {
                c.HorizontalAlignment = edge switch { Dock.Left => HorizontalAlignment.Left, Dock.Right => HorizontalAlignment.Right, _ => HorizontalAlignment.Stretch };
                c.VerticalAlignment = edge switch { Dock.Top => VerticalAlignment.Top, Dock.Bottom => VerticalAlignment.Bottom, _ => VerticalAlignment.Stretch };
                if (edge is Dock.Left or Dock.Right)
                {
                    c.Width = Size;
                }
                else
                {
                    c.Height = Size;
                }

                c.IsHitTestVisible = false;
                c.AttachedToVisualTree += OnAttached;
                c.DetachedFromVisualTree += OnDetached;
                Update(c);
            }
        });
    }

    /// <summary>Gets <see cref="EdgeProperty"/>.</summary>
    public static Dock? GetEdge(Control c) => c.GetValue(EdgeProperty);

    /// <summary>Sets <see cref="EdgeProperty"/>.</summary>
    public static void SetEdge(Control c, Dock? value) => c.SetValue(EdgeProperty, value);

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control c && c.TemplatedParent is ScrollViewer sv)
        {
            EventHandler<ScrollChangedEventArgs> handler = (_, _) => Update(c);
            c.SetValue(HandlerProperty, handler);
            sv.ScrollChanged += handler;
            Update(c);
        }
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control c && c.GetValue(HandlerProperty) is { } handler)
        {
            if (c.TemplatedParent is ScrollViewer sv)
            {
                sv.ScrollChanged -= handler;
            }

            c.ClearValue(HandlerProperty);
        }
    }

    private static void Update(Control c)
    {
        if (c.TemplatedParent is not ScrollViewer sv || GetEdge(c) is not { } edge)
        {
            return;
        }

        var o = sv.Offset;
        var max = new Vector(Math.Max(0, sv.Extent.Width - sv.Viewport.Width), Math.Max(0, sv.Extent.Height - sv.Viewport.Height));
        c.IsVisible = edge switch
        {
            Dock.Left => o.X > 0,
            Dock.Right => o.X < max.X,
            Dock.Top => o.Y > 0,
            _ => o.Y < max.Y,
        };
    }
}
