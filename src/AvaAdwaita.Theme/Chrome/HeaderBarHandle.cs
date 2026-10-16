using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Chrome;

/// <summary>
/// The header bar's <c>windowhandle</c>: dragging it moves the window, as its <c>TitleBar</c> role tells the
/// platform, and a double-click toggles maximize, which Avalonia does not do itself.
/// </summary>
public class HeaderBarHandle : AdwBox
{
    /// <summary>Creates the handle.</summary>
    public HeaderBarHandle()
    {
        Nodes.Css.SetNode(this, "windowhandle");
        Avalonia.Controls.Chrome.WindowDecorationProperties.SetElementRole(this, WindowDecorationsElementRole.TitleBar);
        DoubleTapped += OnDoubleTapped;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Avalonia.Visual v && v.FindAncestorOfType<Button>(includeSelf: true) is not null)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is Window w && w.CanResize && w.CanMaximize)
        {
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }
    }
}
