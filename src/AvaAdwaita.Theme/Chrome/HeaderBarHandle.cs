using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Chrome;

/// <summary>
/// The header bar's <c>windowhandle</c>: dragging it moves the window, as its <c>TitleBar</c> role tells the
/// platform, and a double-click toggles maximize, which Avalonia does not do itself.
/// </summary>
/// <remarks>On macOS, Avalonia.Native ignores the role on ancestors of the hit element, so the handle moves the window itself.</remarks>
public class HeaderBarHandle : AdwBox
{
    /// <summary>
    /// Whether the handles in an element move the window; inherited. Something that drags itself from them, as a bottom
    /// sheet does (AdwSwipeTracker's allow-window-handle), turns it off.
    /// </summary>
    public static readonly AttachedProperty<bool> MovesWindowProperty =
        AvaloniaProperty.RegisterAttached<HeaderBarHandle, Visual, bool>("MovesWindow", true, inherits: true);

    private static readonly bool s_movesItself = OperatingSystem.IsMacOS();

    static HeaderBarHandle()
    {
        MovesWindowProperty.Changed.AddClassHandler<HeaderBarHandle>((h, _) => h.UpdateRole());
    }

    /// <summary>Creates the handle.</summary>
    public HeaderBarHandle()
    {
        Nodes.Css.SetNode(this, "windowhandle");
        UpdateRole();
        if (s_movesItself)
        {
            PointerPressed += OnPointerPressed;
        }
        else
        {
            DoubleTapped += OnDoubleTapped;
        }
    }

    /// <summary>Gets <see cref="MovesWindowProperty"/>.</summary>
    public static bool GetMovesWindow(Visual element) => element.GetValue(MovesWindowProperty);

    /// <summary>Sets <see cref="MovesWindowProperty"/>.</summary>
    public static void SetMovesWindow(Visual element, bool value) => element.SetValue(MovesWindowProperty, value);

    // Clear the role where the handle moves the window itself, so the platform doesn't act twice.
    private void UpdateRole() => Avalonia.Controls.Chrome.WindowDecorationProperties.SetElementRole(
        this, GetMovesWindow(this) && !s_movesItself ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.None);

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Avalonia.Visual v && v.FindAncestorOfType<Button>(includeSelf: true) is not null)
        {
            return;
        }

        if (GetMovesWindow(this) && ToggleMaximize())
        {
            e.Handled = true;
        }
    }

    // The move loop swallows the release, so use the click count rather than DoubleTapped.
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!GetMovesWindow(this) || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || TopLevel.GetTopLevel(this) is not Window w)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else if (w.WindowState != WindowState.FullScreen)
        {
            w.BeginMoveDrag(e);
        }

        e.Handled = true;
    }

    private bool ToggleMaximize()
    {
        if (TopLevel.GetTopLevel(this) is Window w && w.CanResize && w.CanMaximize)
        {
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return true;
        }

        return false;
    }
}
