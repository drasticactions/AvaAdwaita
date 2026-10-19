using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;
using AvaAdwaita.Animation;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// Shows and hides its child with an animation (GtkRevealer): a slide grows the revealer's size along one direction
/// while the child keeps its own size, and a crossfade fades the child. A hidden child is not shown at all.
/// </summary>
public class Revealer : Control
{
    /// <summary>Defines <see cref="Child"/>.</summary>
    public static readonly StyledProperty<Control?> ChildProperty =
        Decorator.ChildProperty.AddOwner<Revealer>();

    /// <summary>Defines <see cref="IsRevealed"/>.</summary>
    public static readonly StyledProperty<bool> IsRevealedProperty =
        AvaloniaProperty.Register<Revealer, bool>(nameof(IsRevealed), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="Transition"/>.</summary>
    public static readonly StyledProperty<RevealerTransition> TransitionProperty =
        AvaloniaProperty.Register<Revealer, RevealerTransition>(nameof(Transition), RevealerTransition.SlideDown);

    /// <summary>Defines <see cref="TransitionDuration"/>.</summary>
    public static readonly StyledProperty<TimeSpan> TransitionDurationProperty =
        AvaloniaProperty.Register<Revealer, TimeSpan>(nameof(TransitionDuration), TimeSpan.FromMilliseconds(250));

    /// <summary>Defines <see cref="IsChildRevealed"/>.</summary>
    public static readonly DirectProperty<Revealer, bool> IsChildRevealedProperty =
        AvaloniaProperty.RegisterDirect<Revealer, bool>(nameof(IsChildRevealed), r => r.IsChildRevealed);

    // The revealer node's box: it draws the node's style and holds the child.
    private readonly Presenters.AdwBox _host = new() { AppliesEffects = false };
    private TimedAnimation? _animation;
    private IDisposable? _childHidden;
    private NodeEffects? _effects;
    private double _position;
    private bool _isChildRevealed;

    static Revealer()
    {
        AffectsMeasure<Revealer>(TransitionProperty);
    }

    /// <summary>Creates a revealer. It is a <c>revealer</c> node, as GtkRevealer.</summary>
    public Revealer()
    {
        SetValue(Nodes.Css.NodeProperty, "revealer", BindingPriority.Style);
        _host.IsVisible = false;
        VisualChildren.Add(_host);
        LogicalChildren.Add(_host);
    }

    /// <summary>The child.</summary>
    [Content]
    public Control? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <summary>Whether the child shows, or is on its way to.</summary>
    public bool IsRevealed
    {
        get => GetValue(IsRevealedProperty);
        set => SetValue(IsRevealedProperty, value);
    }

    /// <summary>How the child shows and hides.</summary>
    public RevealerTransition Transition
    {
        get => GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    /// <summary>How long showing or hiding takes.</summary>
    public TimeSpan TransitionDuration
    {
        get => GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    /// <summary>Whether the child has finished showing.</summary>
    public bool IsChildRevealed
    {
        get => _isChildRevealed;
        private set => SetAndRaise(IsChildRevealedProperty, ref _isChildRevealed, value);
    }

    /// <summary>How far the child shows, 0 to 1.</summary>
    internal double Position => _position;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The node's own opacity, filters and transforms, as a GTK widget's.
        _effects = new NodeEffects(this);
        if (Css.GetStyle(this) is { } style)
        {
            _effects.Push(style);
        }

        _animation ??= new TimedAnimation(this, TransitionDuration.TotalMilliseconds, AdwEasing.EaseOutCubic, SetPosition);
        _animation.Jump(IsRevealed ? 1 : 0);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _effects?.Dispose();
        _effects = null;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Css.StyleProperty && Css.GetStyle(this) is { } style)
        {
            _effects?.Push(style);
        }

        if (change.Property == ChildProperty)
        {
            _childHidden?.Dispose();
            _childHidden = null;
            _host.Child = change.GetNewValue<Control?>();
            UpdateChildVisible();
        }
        else if (change.Property == IsRevealedProperty)
        {
            var target = change.GetNewValue<bool>() ? 1 : 0;
            if (_animation is null || Transition == RevealerTransition.None)
            {
                _animation?.Jump(target);
                SetPosition(target);
            }
            else
            {
                _animation.Animate(target);
            }
        }
        else if (change.Property == TransitionDurationProperty && _animation is not null)
        {
            _animation = new TimedAnimation(this, TransitionDuration.TotalMilliseconds, AdwEasing.EaseOutCubic, SetPosition);
            _animation.Jump(_position);
        }
    }

    // gtk_revealer_set_position.
    private void SetPosition(double position)
    {
        _position = position;
        _host.IsVisible = position != 0;
        UpdateChildVisible();
        // GTK clips the child only while it slides.
        ClipToBounds = position is > 0 and < 1;
        var fades = Transition == RevealerTransition.Crossfade;
        _host.Opacity = fades ? position : 1;
        IsChildRevealed = position == 1 && IsRevealed;
        InvalidateMeasure();
    }

    // A hidden child is not shown at all, as GTK's child-visible: above whatever the application sets, which comes back
    // when the child shows again.
    private void UpdateChildVisible()
    {
        if (_position == 0 && _childHidden is null && Child is { } child)
        {
            _childHidden = child.SetValue(IsVisibleProperty, false, BindingPriority.Animation);
        }
        else if (_position != 0)
        {
            _childHidden?.Dispose();
            _childHidden = null;
        }
    }

    private (double H, double V) Scale() => Transition switch
    {
        RevealerTransition.SlideLeft or RevealerTransition.SlideRight => (_position, 1),
        RevealerTransition.SlideUp or RevealerTransition.SlideDown => (1, _position),
        _ => (1, 1),
    };

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_host.Child is not { } child)
        {
            return default;
        }

        if (!_host.IsVisible)
        {
            // gtk_revealer_measure measures a hidden child too, scaled only along the slide: a vertical slide keeps
            // its width at no height, a horizontal one its height, and a crossfade its whole size.
            var (sh, sv) = Scale();
            if (sh == 0 && sv == 0)
            {
                return default;
            }

            _childHidden?.Dispose();
            child.Measure(new Size(sh == 0 ? double.PositiveInfinity : availableSize.Width, sv == 0 ? double.PositiveInfinity : availableSize.Height));
            var hidden = child.DesiredSize;
            _childHidden = child.SetValue(IsVisibleProperty, false, BindingPriority.Animation);
            return new Size(Math.Ceiling(hidden.Width * sh), Math.Ceiling(hidden.Height * sv));
        }

        var (h, v) = Scale();
        _host.Measure(new Size(h < 1 ? double.PositiveInfinity : availableSize.Width, v < 1 ? double.PositiveInfinity : availableSize.Height));
        return new Size(Math.Ceiling(_host.DesiredSize.Width * h), Math.Ceiling(_host.DesiredSize.Height * v));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (h, v) = Scale();
        var width = h < 1 ? _host.DesiredSize.Width : finalSize.Width;
        var height = v < 1 ? _host.DesiredSize.Height : finalSize.Height;

        // A slide toward the end keeps the child against the far edge: the slide-down's bottom shows first.
        var x = Transition == RevealerTransition.SlideRight && h < 1 ? finalSize.Width - width : 0;
        var y = Transition == RevealerTransition.SlideDown && v < 1 ? finalSize.Height - height : 0;
        _host.Arrange(new Rect(x, y, width, height));
        return finalSize;
    }
}
