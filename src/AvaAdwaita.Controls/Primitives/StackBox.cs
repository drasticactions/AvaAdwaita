using System.Linq;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>A GtkStack without transitions: a <c>stack</c> node showing one of its children.</summary>
public class StackBox : Panel
{
    /// <summary>Defines <see cref="VisibleChild"/>.</summary>
    public static readonly StyledProperty<Control?> VisibleChildProperty =
        AvaloniaProperty.Register<StackBox, Control?>(nameof(VisibleChild));

    /// <summary>Creates a stack.</summary>
    public StackBox()
    {
        Css.SetNode(this, "stack");
        Children.CollectionChanged += (_, _) => Update();
    }

    /// <summary>The child that shows; the first when unset.</summary>
    public Control? VisibleChild
    {
        get => GetValue(VisibleChildProperty);
        set => SetValue(VisibleChildProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisibleChildProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        var visible = VisibleChild is { } v && Children.Contains(v) ? v : Children.FirstOrDefault();
        foreach (var c in Children)
        {
            c.IsVisible = ReferenceEquals(c, visible);
        }
    }
}
