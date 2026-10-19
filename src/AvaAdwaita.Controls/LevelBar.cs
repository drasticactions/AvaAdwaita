using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls;

/// <summary>
/// A bar that shows a level (GtkLevelBar), as a battery's charge or a password's strength. Its filled blocks take the
/// class of the first offset at or above the value: <c>low</c>, <c>high</c> or <c>full</c> by default, at 0.25, 0.75
/// and 1. <see cref="SetOffset"/> adds or moves an offset, for a class the application styles.
/// </summary>
public class LevelBar : RangeBase
{
    /// <summary>Defines <see cref="Mode"/>.</summary>
    public static readonly StyledProperty<LevelBarMode> ModeProperty =
        AvaloniaProperty.Register<LevelBar, LevelBarMode>(nameof(Mode));

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<LevelBar, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>Defines <see cref="IsInverted"/>.</summary>
    public static readonly StyledProperty<bool> IsInvertedProperty =
        AvaloniaProperty.Register<LevelBar, bool>(nameof(IsInverted));

    private readonly List<(string Name, double Value)> _offsets = [("low", 0.25), ("high", 0.75), ("full", 1)];

    static LevelBar()
    {
        MaximumProperty.OverrideDefaultValue<LevelBar>(1);
        CssNodeProviders.Register<LevelBar>(
            b => [b.Orientation == Orientation.Horizontal ? "horizontal" : "vertical", b.Mode == LevelBarMode.Continuous ? "continuous" : "discrete"],
            ModeProperty,
            OrientationProperty);
    }

    /// <summary>Raised when an offset is added, moved or removed.</summary>
    public event System.EventHandler? OffsetsChanged;

    /// <summary>Whether the bar is one block or a block per unit.</summary>
    public LevelBarMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>The direction the bar fills in.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Fills from the end rather than the start.</summary>
    public bool IsInverted
    {
        get => GetValue(IsInvertedProperty);
        set => SetValue(IsInvertedProperty, value);
    }

    /// <summary>The offsets, in increasing order of value.</summary>
    public IReadOnlyList<(string Name, double Value)> Offsets => _offsets;

    /// <summary>Adds an offset, or moves one that has the name (gtk_level_bar_add_offset_value).</summary>
    public void SetOffset(string name, double value)
    {
        _offsets.RemoveAll(o => o.Name == name);
        _offsets.Add((name, value));
        _offsets.Sort((a, b) => a.Value.CompareTo(b.Value));
        OffsetsChanged?.Invoke(this, System.EventArgs.Empty);
    }

    /// <summary>Removes an offset.</summary>
    public void RemoveOffset(string name)
    {
        if (_offsets.RemoveAll(o => o.Name == name) > 0)
        {
            OffsetsChanged?.Invoke(this, System.EventArgs.Empty);
        }
    }

    /// <summary>The class the filled blocks take for the value (update_level_style_classes).</summary>
    internal string? ValueClass()
    {
        for (var i = 0; i < _offsets.Count; i++)
        {
            if (Value <= _offsets[i].Value && (i == 0 || _offsets[i - 1].Value < Value))
            {
                return _offsets[i].Name;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.LevelBarAutomationPeer(this);
}
