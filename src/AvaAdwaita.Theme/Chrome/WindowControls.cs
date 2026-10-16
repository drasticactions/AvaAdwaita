using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Chrome;

/// <summary>
/// The window buttons of one side of the header bar, as <c>windowcontrols.start</c> or <c>.end</c>, in the
/// order of the desktop's button layout. Minimize hides when the window cannot minimize, maximize when it
/// cannot resize.
/// </summary>
public class WindowControls : StackPanel
{
    /// <summary>Defines <see cref="IsStart"/>.</summary>
    public static readonly StyledProperty<bool> IsStartProperty =
        AvaloniaProperty.Register<WindowControls, bool>(nameof(IsStart));

    private Window? _window;

    static WindowControls()
    {
        IsStartProperty.Changed.AddClassHandler<WindowControls>((c, _) => c.Rebuild());
        Nodes.Css.StyleProperty.Changed.AddClassHandler<WindowControls>((c, _) => c.OnStyleChanged());
    }

    /// <summary>Creates the controls.</summary>
    public WindowControls()
    {
        Orientation = Orientation.Horizontal;
        Nodes.Css.SetNode(this, "windowcontrols");
    }

    /// <summary>True for the start side, false for the end side.</summary>
    public bool IsStart
    {
        get => GetValue(IsStartProperty);
        set => SetValue(IsStartProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = this.FindAncestorOfType<Window>() ?? TopLevel.GetTopLevel(this) as Window;
        if (_window is not null)
        {
            _window.PropertyChanged += OnWindowPropertyChanged;
        }

        AdwaitaEnvironment.Changed += OnEnvironmentChanged;
        Rebuild();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window is not null)
        {
            _window.PropertyChanged -= OnWindowPropertyChanged;
        }

        AdwaitaEnvironment.Changed -= OnEnvironmentChanged;
        _window = null;
    }

    private void OnEnvironmentChanged(Settings.StyleStateChange change)
    {
        if ((change & Settings.StyleStateChange.ButtonLayout) != 0)
        {
            Rebuild();
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty || e.Property == Window.CanResizeProperty || e.Property == Window.CanMinimizeProperty
            || e.Property == Window.CanMaximizeProperty || e.Property == WindowChrome.IsClientSideProperty
            || e.Property == HeaderBar.ShowStartTitleButtonsProperty || e.Property == HeaderBar.ShowEndTitleButtonsProperty)
        {
            Rebuild();
        }
    }

    private void OnStyleChanged()
    {
        if (GetValue(Nodes.Css.StyleProperty) is { } s)
        {
            Spacing = s.Size.BorderSpacing.X.Value;
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        var w = _window;
        var show = w is not null && WindowChrome.GetIsClientSide(w) && (IsStart ? HeaderBar.GetShowStartTitleButtons(w) : HeaderBar.GetShowEndTitleButtons(w));
        var layout = ButtonLayout.Parse(AdwaitaEnvironment.IsInitialized ? AdwaitaEnvironment.State.ButtonLayout : null);
        var names = IsStart ? layout.Start : layout.End;
        if (show)
        {
            foreach (var name in names)
            {
                if (name == "minimize" && !w!.CanMinimize)
                {
                    continue;
                }

                if (name == "maximize" && (!w!.CanResize || !w.CanMaximize))
                {
                    continue;
                }

                Children.Add(CreateButton(name, w!));
            }
        }

        Nodes.Css.SetClasses(this, (IsStart ? "start" : "end") + (Children.Count == 0 ? " empty" : string.Empty));

        // GtkWindowControls hides itself when it has no buttons.
        IsVisible = Children.Count > 0;
    }

    private static Button CreateButton(string name, Window w)
    {
        var icon = name switch
        {
            "minimize" => "window-minimize-symbolic",
            "maximize" => w.WindowState == WindowState.Maximized ? "window-restore-symbolic" : "window-maximize-symbolic",
            _ => "window-close-symbolic",
        };
        var image = new AdwIcon { IconName = icon };
        Nodes.Css.SetNode(image, "image");
        var b = new Button { Content = image, Focusable = false };
        b.Classes.Add(name);
        b.SetValue(WindowDecorationProperties.ElementRoleProperty, name switch
        {
            "minimize" => WindowDecorationsElementRole.MinimizeButton,
            "maximize" => WindowDecorationsElementRole.MaximizeButton,
            _ => WindowDecorationsElementRole.CloseButton,
        });
        b.Click += (_, _) =>
        {
            switch (name)
            {
                case "minimize":
                    w.WindowState = WindowState.Minimized;
                    break;
                case "maximize":
                    w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    break;
                default:
                    w.Close();
                    break;
            }
        };
        return b;
    }
}
