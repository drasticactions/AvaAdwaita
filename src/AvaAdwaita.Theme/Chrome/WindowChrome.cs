using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Computed;

namespace AvaAdwaita.Chrome;

/// <summary>
/// Makes a window look like a libadwaita window: the <c>.csd</c> (or <c>.ssd</c>), <c>.maximized</c> and <c>.fullscreen</c>
/// classes, the shadow space from the window's <c>box-shadow</c>, and the button layout.
/// </summary>
public static class WindowChrome
{
    /// <summary>Set by the Window theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("IsEnabled", typeof(WindowChrome));

    /// <summary>The margin around the window frame: the shadow space while the theme draws the decorations.</summary>
    public static readonly AttachedProperty<Thickness> FrameMarginProperty =
        AvaloniaProperty.RegisterAttached<Window, Thickness>("FrameMargin", typeof(WindowChrome));

    /// <summary>True while the theme draws the window decorations and the window buttons show.</summary>
    public static readonly AttachedProperty<bool> IsClientSideProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("IsClientSide", typeof(WindowChrome));

    private static readonly ConditionalWeakTable<Window, Tracker> s_trackers = new();

    static WindowChrome()
    {
        IsEnabledProperty.Changed.AddClassHandler<Window>((w, e) =>
        {
            if (e.NewValue is true && !s_trackers.TryGetValue(w, out _))
            {
                s_trackers.Add(w, new Tracker(w));
            }
        });
    }

    /// <summary>
    /// Draw client-side decorations even where the platform does not ask for them, as the headless
    /// platform: the shadow then sits inside the window. For tests and screenshots.
    /// </summary>
    public static bool AssumeClientSide { get; set; }

    /// <summary>Gets <see cref="IsEnabledProperty"/>.</summary>
    public static bool GetIsEnabled(Window w) => w.GetValue(IsEnabledProperty);

    /// <summary>Sets <see cref="IsEnabledProperty"/>.</summary>
    public static void SetIsEnabled(Window w, bool v) => w.SetValue(IsEnabledProperty, v);

    /// <summary>Gets <see cref="FrameMarginProperty"/>.</summary>
    public static Thickness GetFrameMargin(Window w) => w.GetValue(FrameMarginProperty);

    /// <summary>Gets <see cref="IsClientSideProperty"/>.</summary>
    public static bool GetIsClientSide(Window w) => w.GetValue(IsClientSideProperty);

    /// <summary>The window's drawn decorations, when the platform asked for them.</summary>
    internal static WindowDrawnDecorations? Decorations(Window w) =>
        w.GetVisualParent() is ILogical host ? host.LogicalChildren.OfType<WindowDrawnDecorations>().FirstOrDefault() : null;

    private sealed class Tracker
    {
        private readonly Window _window;

        public Tracker(Window window)
        {
            _window = window;
            window.PropertyChanged += OnPropertyChanged;
            window.Opened += (_, _) => Update();
            AdwaitaEnvironment.Changed += _ => Update();
            Update();
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Window.WindowStateProperty || e.Property == Window.WindowDecorationMarginProperty
                || e.Property == Window.IsExtendedIntoWindowDecorationsProperty || e.Property == Nodes.Css.StyleProperty
                || e.Property == Window.CanResizeProperty)
            {
                Update();
            }
        }

        private void Update()
        {
            var decorations = Decorations(_window);
            // Drawn decorations mean client-side decorations whether or not the app extended into them: with the
            // title bar height at 0 the window's own header bar is the title bar in both cases.
            var csd = decorations is not null || AssumeClientSide;
            var state = _window.WindowState;
            var classes = new List<string> { "background" };
            // libadwaita's stylesheet styles window.ssd (a hairline around a window the system decorates).
            classes.Add(csd ? "csd" : "ssd");

            if (state == WindowState.Maximized)
            {
                classes.Add("maximized");
            }
            else if (state == WindowState.FullScreen)
            {
                classes.Add("fullscreen");
            }

            var text = string.Join(' ', classes);
            if (Nodes.Css.GetClasses(_window) != text)
            {
                Nodes.Css.SetClasses(_window, text);
            }

            _window.SetValue(IsClientSideProperty, csd);
            var style = _window.GetValue(Nodes.Css.StyleProperty);
            var shadow = style is not null && csd && state == WindowState.Normal ? ShadowExtents.Of(style.Background.BoxShadow) : default;
            if (decorations is not null)
            {
                decorations.DefaultShadowThickness = shadow;
                decorations.DefaultFrameThickness = default;
                decorations.DefaultTitleBarHeight = 0;
            }

            var margin = decorations is not null && _window.IsExtendedIntoWindowDecorations ? _window.WindowDecorationMargin : AssumeClientSide ? shadow : default;
            _window.SetValue(FrameMarginProperty, margin);
        }
    }

    internal static ComputedStyle? StyleOf(Window w) => w.GetValue(Nodes.Css.StyleProperty);
}
