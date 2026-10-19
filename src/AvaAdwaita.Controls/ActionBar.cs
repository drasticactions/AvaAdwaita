using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaAdwaita.Controls;

/// <summary>
/// A bar of actions at the bottom of a view (GtkActionBar): widgets at the start, at the end (packed from the end), and
/// one in the middle. It slides up when <see cref="IsRevealed"/> turns on.
/// </summary>
public class ActionBar : TemplatedControl
{
    /// <summary>Defines <see cref="CenterWidget"/>.</summary>
    public static readonly StyledProperty<object?> CenterWidgetProperty =
        AvaloniaProperty.Register<ActionBar, object?>(nameof(CenterWidget));

    /// <summary>Defines <see cref="IsRevealed"/>.</summary>
    public static readonly StyledProperty<bool> IsRevealedProperty =
        AvaloniaProperty.Register<ActionBar, bool>(nameof(IsRevealed), true);

    private readonly Avalonia.Controls.Controls _endShown = new();

    /// <summary>Creates an action bar.</summary>
    public ActionBar()
    {
        End.CollectionChanged += (_, _) =>
        {
            _endShown.Clear();
            _endShown.AddRange(End.Reverse().ToList());
        };
    }

    /// <summary>Widgets at the start, from the start.</summary>
    public Avalonia.Controls.Controls Start { get; } = new();

    /// <summary>Widgets at the end, from the end: the first is the last on the bar, as gtk_action_bar_pack_end.</summary>
    public Avalonia.Controls.Controls End { get; } = new();

    /// <summary>A widget in the middle.</summary>
    public object? CenterWidget
    {
        get => GetValue(CenterWidgetProperty);
        set => SetValue(CenterWidgetProperty, value);
    }

    /// <summary>Whether the bar shows.</summary>
    public bool IsRevealed
    {
        get => GetValue(IsRevealedProperty);
        set => SetValue(IsRevealedProperty, value);
    }

    /// <summary>The end widgets in the order they show, from the start.</summary>
    internal Avalonia.Controls.Controls EndShown => _endShown;

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ActionBarAutomationPeer(this);
}
