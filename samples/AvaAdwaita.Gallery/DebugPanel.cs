#if DEBUG
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Gallery;

/// <summary>
/// Ctrl+Shift+I shows the CSS node under the pointer: its path, the rules that match it in cascade order,
/// and its computed properties that differ from the initial values.
/// </summary>
internal sealed class DebugPanel
{
    private readonly Window _window;
    private readonly Popup _popup;
    private readonly TextBlock _text = new() { FontFamily = new FontFamily("monospace"), FontSize = 11, TextWrapping = TextWrapping.Wrap };
    private readonly CssEngine.Computed.ComputedStyle _initial = InitialStyle();
    private bool _shown;

    private DebugPanel(Window window)
    {
        _window = window;
        _popup = new Popup
        {
            PlacementTarget = window,
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopRight,
            PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomLeft,
            IsLightDismissEnabled = false,
            Child = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 30, 30, 30)),
                Padding = new Thickness(8),
                Width = 520,
                MaxHeight = 640,
                Child = new ScrollViewer { Content = _text },
            },
        };
        _text.Foreground = Brushes.White;
        ((Panel)window.Content!).Children.Add(_popup);
        window.KeyDown += OnKeyDown;
        window.PointerMoved += OnPointerMoved;
    }

    private static CssEngine.Computed.ComputedStyle InitialStyle()
    {
        var engine = new CssStyleEngine(new CssEngine.Cascade.CssStyleSet());
        var node = new CssNode("initial");
        engine.AddRoot(node);
        return engine.Resolve(node);
    }

    public static void Attach(Window window) => _ = new DebugPanel(window);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.I && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            _shown = !_shown;
            _popup.IsOpen = _shown;
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_shown)
        {
            return;
        }

        var hit = _window.InputHitTest(e.GetPosition(_window)) as Visual;
        var node = hit?.GetSelfAndVisualAncestors().Select(v => Css.GetCssNode(v)).FirstOrDefault(n => n is not null);
        if (node is null)
        {
            _text.Text = "(no node)";
            return;
        }

        var engine = AdwaitaEnvironment.Engine;
        var style = engine.Resolve(node);
        var rules = engine.DescribeMatches(node);
        _text.Text = node.Path + "\n\nRules:\n" + string.Join("\n", rules) + "\n\nComputed:\n" + style.Describe(_initial);
    }
}
#endif
