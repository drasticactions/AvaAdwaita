using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using AvaAdwaita.Resources;

namespace AvaAdwaita.Controls;

/// <summary>
/// A keyboard shortcut drawn as keycaps (AdwShortcutLabel). <see cref="Accelerator"/> takes GTK's accelerator syntax:
/// <c>&lt;Control&gt;q</c> for one shortcut, spaces between alternatives (shown with "/"), <c>&amp;</c> between
/// keys pressed together, <c>+</c> between the steps of a sequence (shown with "→") and <c>...</c> for a range
/// (shown with "⋯"). With no accelerator it shows <see cref="DisabledText"/>.
/// </summary>
public class ShortcutLabel : Control
{
    /// <summary>Defines <see cref="Accelerator"/>.</summary>
    public static readonly StyledProperty<string> AcceleratorProperty =
        AvaloniaProperty.Register<ShortcutLabel, string>(nameof(Accelerator), string.Empty);

    /// <summary>Defines <see cref="DisabledText"/>.</summary>
    public static readonly StyledProperty<string> DisabledTextProperty =
        AvaloniaProperty.Register<ShortcutLabel, string>(nameof(DisabledText), string.Empty);

    private const string Context = "keyboard label";

    private readonly AdwStack _box = new() { Orientation = Orientation.Horizontal };
    private readonly WrapBox _wrap = new() { ChildSpacing = 6, LineSpacing = 6 };
    private Panel _panel;
    private bool _isWrapping;

    /// <summary>Creates a shortcut label: a <c>shortcut-label</c> node.</summary>
    public ShortcutLabel()
    {
        Css.SetNode(this, "shortcut-label");
        _wrap.SetValue(Css.NodeProperty, null);
        _panel = _box;
        VisualChildren.Add(_panel);
        LogicalChildren.Add(_panel);
        Rebuild();
    }

    /// <summary>The accelerator shown, in GTK's syntax.</summary>
    public string Accelerator
    {
        get => GetValue(AcceleratorProperty);
        set => SetValue(AcceleratorProperty, value);
    }

    /// <summary>The text shown when there is no accelerator.</summary>
    public string DisabledText
    {
        get => GetValue(DisabledTextProperty);
        set => SetValue(DisabledTextProperty, value);
    }

    /// <summary>Wraps the keys onto lines, six apart, as a shortcuts dialog's rows do (adw_shortcut_label_set_wrap).</summary>
    internal bool IsWrapping
    {
        get => _isWrapping;
        set
        {
            if (_isWrapping == value)
            {
                return;
            }

            _isWrapping = value;
            VisualChildren.Remove(_panel);
            LogicalChildren.Remove(_panel);
            _panel.Children.Clear();
            _panel = value ? _wrap : _box;
            VisualChildren.Add(_panel);
            LogicalChildren.Add(_panel);
            Rebuild();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ShortcutLabelAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AcceleratorProperty || change.Property == DisabledTextProperty || change.Property == FlowDirectionProperty)
        {
            Rebuild();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _panel.Measure(availableSize);
        return _panel.DesiredSize;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        _panel.Arrange(new Rect(finalSize));
        return finalSize;
    }

    // rebuild.
    private void Rebuild()
    {
        var children = _panel.Children;
        children.Clear();
        if (string.IsNullOrEmpty(Accelerator))
        {
            children.Add(Dim(DisabledText));
            AutomationProperties.SetName(this, null);
            return;
        }

        var accessible = new StringBuilder();
        var parts = Accelerator.Split(' ');
        for (var k = 0; k < parts.Length; k++)
        {
            if (k > 0)
            {
                children.Add(Dim("/"));
                accessible.Append(' ');
            }

            if (!ParseRange(parts[k], accessible))
            {
                break;
            }
        }

        AutomationProperties.SetName(this, accessible.ToString());
    }

    private bool ParseRange(string str, StringBuilder accessible)
    {
        var dots = str.IndexOf("...", StringComparison.Ordinal);
        if (dots < 0)
        {
            return ParseSequence(str, accessible);
        }

        if (!ParseSequence(str[..dots], accessible))
        {
            return false;
        }

        _panel.Children.Add(Dim("⋯"));
        accessible.Append('…');
        return ParseSequence(str[(dots + 3)..], accessible);
    }

    private bool ParseSequence(string str, StringBuilder accessible)
    {
        var steps = str.Split('+');
        for (var k = 0; k < steps.Length; k++)
        {
            if (k > 0)
            {
                var arrow = FlowDirection == FlowDirection.RightToLeft ? "←" : "→";
                _panel.Children.Add(Dim(arrow));
                accessible.Append(arrow);
            }

            if (!ParseCombination(steps[k], accessible))
            {
                return false;
            }
        }

        return true;
    }

    private bool ParseCombination(string str, StringBuilder accessible)
    {
        foreach (var accel in str.Split('&'))
        {
            if (Accelerators.Parse(accel) is not { } parsed)
            {
                return false;
            }

            // display_shortcut: the keys in a box, left to right whatever the direction.
            var box = new AdwStack { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };
            Css.SetNode(box, "box");
            Css.SetClasses(box, "horizontal");
            foreach (var label in Labels(parsed.Key, parsed.Modifiers))
            {
                var keycap = new MarkupLabel { Markup = label, TextWrapping = TextWrapping.NoWrap };
                Css.SetClasses(keycap, "keycap");
                box.Children.Add(keycap);
            }

            _panel.Children.Add(box);
            accessible.Append(Accelerators.Label(parsed.Key, parsed.Modifiers));
        }

        return true;
    }

    private static AdwLabel Dim(string text)
    {
        var label = new AdwLabel { Text = text };
        Css.SetNode(label, "label");
        Css.SetClasses(label, "dimmed");
        return label;
    }

    private static string Key(string label) => GtkCatalog.Pgettext(Context, label);

    // get_labels: the modifiers in libadwaita's order, then the key.
    internal static List<string> Labels(string key, GdkModifiers modifiers)
    {
        var labels = new List<string>();
        if (modifiers.HasFlag(GdkModifiers.Hyper))
        {
            labels.Add(Key("Hyper"));
        }

        if (modifiers.HasFlag(GdkModifiers.Super))
        {
            labels.Add(Key("Super"));
        }

        if (modifiers.HasFlag(GdkModifiers.Control))
        {
            labels.Add(Key("Ctrl"));
        }

        if (modifiers.HasFlag(GdkModifiers.Alt))
        {
            labels.Add(Key("Alt"));
        }

        if (modifiers.HasFlag(GdkModifiers.Shift))
        {
            labels.Add(Key("Shift"));
        }

        if (modifiers.HasFlag(GdkModifiers.Meta))
        {
            labels.Add(Key("Meta"));
        }

        if (Accelerators.Character(key) is { } ch && ch < 0x80 && ch > 0x20 && ch != 0x7f)
        {
            labels.Add(ch switch
            {
                '<' => "&lt;",
                '>' => "&gt;",
                '&' => "&amp;",
                '"' => "&quot;",
                '\'' => "&apos;",
                '\\' => Key("Backslash"),
                _ => (key.StartsWith("KP_", StringComparison.Ordinal) ? "KP " : string.Empty) + char.ToUpperInvariant(ch),
            });
            return labels;
        }

        var side = key.EndsWith("_L", StringComparison.Ordinal) ? "L" : key.EndsWith("_R", StringComparison.Ordinal) ? "R" : null;
        var modifier = side is null ? null : key[..^2] switch
        {
            "Shift" => "Shift",
            "Control" => "Ctrl",
            "Alt" => "Alt",
            "Meta" => "Meta",
            "Super" => "Super",
            "Hyper" => "Hyper",
            _ => null,
        };
        if (modifier is not null)
        {
            // A four-per-em space, as libadwaita's.
            labels.Add($"{Key(modifier)}\u2005<small><b>{GtkCatalog.Pgettext("keyboard side marker", side!)}</b></small>");
            return labels;
        }

        labels.Add(key switch
        {
            "Left" => "←",
            "Up" => "↑",
            "Right" => "→",
            "Down" => "↓",
            "space" => "␣",
            "Return" => "⏎",
            _ when key.Length == 1 => char.ToUpperInvariant(key[0]).ToString(),
            _ => Key(key),
        });
        return labels;
    }
}
