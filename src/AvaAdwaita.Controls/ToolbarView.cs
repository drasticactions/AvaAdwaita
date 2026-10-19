using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Metadata;
using AvaAdwaita.Chrome;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls;

/// <summary>
/// Content with bars above and below it (AdwToolbarView), as a header bar on top and an action bar at the bottom. As a
/// window's content it replaces the window's own header bar, as in an AdwApplicationWindow.
/// </summary>
public class ToolbarView : TemplatedControl
{
    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<ToolbarView>();

    /// <summary>Defines <see cref="TopBarStyle"/>.</summary>
    public static readonly StyledProperty<ToolbarStyle> TopBarStyleProperty =
        AvaloniaProperty.Register<ToolbarView, ToolbarStyle>(nameof(TopBarStyle));

    /// <summary>Defines <see cref="BottomBarStyle"/>.</summary>
    public static readonly StyledProperty<ToolbarStyle> BottomBarStyleProperty =
        AvaloniaProperty.Register<ToolbarView, ToolbarStyle>(nameof(BottomBarStyle));

    /// <summary>Defines <see cref="RevealTopBars"/>.</summary>
    public static readonly StyledProperty<bool> RevealTopBarsProperty =
        AvaloniaProperty.Register<ToolbarView, bool>(nameof(RevealTopBars), true);

    /// <summary>Defines <see cref="RevealBottomBars"/>.</summary>
    public static readonly StyledProperty<bool> RevealBottomBarsProperty =
        AvaloniaProperty.Register<ToolbarView, bool>(nameof(RevealBottomBars), true);

    /// <summary>Defines <see cref="ExtendContentToTopEdge"/>.</summary>
    public static readonly StyledProperty<bool> ExtendContentToTopEdgeProperty =
        AvaloniaProperty.Register<ToolbarView, bool>(nameof(ExtendContentToTopEdge));

    /// <summary>Defines <see cref="ExtendContentToBottomEdge"/>.</summary>
    public static readonly StyledProperty<bool> ExtendContentToBottomEdgeProperty =
        AvaloniaProperty.Register<ToolbarView, bool>(nameof(ExtendContentToBottomEdge));

    /// <summary>Defines <see cref="TopBarHeight"/>.</summary>
    public static readonly DirectProperty<ToolbarView, double> TopBarHeightProperty =
        AvaloniaProperty.RegisterDirect<ToolbarView, double>(nameof(TopBarHeight), v => v.TopBarHeight);

    /// <summary>Defines <see cref="BottomBarHeight"/>.</summary>
    public static readonly DirectProperty<ToolbarView, double> BottomBarHeightProperty =
        AvaloniaProperty.RegisterDirect<ToolbarView, double>(nameof(BottomBarHeight), v => v.BottomBarHeight);

    private double _topBarHeight;
    private double _bottomBarHeight;
    private IDisposable? _windowBar;
    private Control? _topBar;
    private Control? _bottomBar;
    private Control? _topBox;
    private Control? _bottomBox;
    private Control? _layout;

    static ToolbarView()
    {
        CssNodeProviders.Register<ToolbarView>(
            v =>
            [
                .. v.TopBarStyle == ToolbarStyle.Flat && !v.ExtendContentToTopEdge && v.TopBarHeight > 0 ? ["undershoot-top"] : Array.Empty<string>(),
                .. v.BottomBarStyle == ToolbarStyle.Flat && !v.ExtendContentToBottomEdge && v.BottomBarHeight > 0 ? ["undershoot-bottom"] : Array.Empty<string>(),
            ],
            TopBarStyleProperty,
            BottomBarStyleProperty,
            ExtendContentToTopEdgeProperty,
            ExtendContentToBottomEdgeProperty,
            TopBarHeightProperty,
            BottomBarHeightProperty);
    }

    /// <summary>Creates a toolbar view.</summary>
    public ToolbarView()
    {
        TopBars.CollectionChanged += (_, _) => UpdateClasses();
        BottomBars.CollectionChanged += (_, _) => UpdateClasses();
    }

    /// <summary>The content between the bars.</summary>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The bars above the content, as header bars, a tab bar or a search bar.</summary>
    public Avalonia.Controls.Controls TopBars { get; } = new();

    /// <summary>The bars below the content, as action bars or a view switcher bar.</summary>
    public Avalonia.Controls.Controls BottomBars { get; } = new();

    /// <summary>How the top bars look.</summary>
    public ToolbarStyle TopBarStyle
    {
        get => GetValue(TopBarStyleProperty);
        set => SetValue(TopBarStyleProperty, value);
    }

    /// <summary>How the bottom bars look.</summary>
    public ToolbarStyle BottomBarStyle
    {
        get => GetValue(BottomBarStyleProperty);
        set => SetValue(BottomBarStyleProperty, value);
    }

    /// <summary>Whether the top bars show; they slide out of the way when it turns off.</summary>
    public bool RevealTopBars
    {
        get => GetValue(RevealTopBarsProperty);
        set => SetValue(RevealTopBarsProperty, value);
    }

    /// <summary>Whether the bottom bars show; they slide out of the way when it turns off.</summary>
    public bool RevealBottomBars
    {
        get => GetValue(RevealBottomBarsProperty);
        set => SetValue(RevealBottomBarsProperty, value);
    }

    /// <summary>The content goes under the top bars, to the top edge.</summary>
    public bool ExtendContentToTopEdge
    {
        get => GetValue(ExtendContentToTopEdgeProperty);
        set => SetValue(ExtendContentToTopEdgeProperty, value);
    }

    /// <summary>The content goes under the bottom bars, to the bottom edge.</summary>
    public bool ExtendContentToBottomEdge
    {
        get => GetValue(ExtendContentToBottomEdgeProperty);
        set => SetValue(ExtendContentToBottomEdgeProperty, value);
    }

    /// <summary>The height the top bars take now.</summary>
    public double TopBarHeight
    {
        get => _topBarHeight;
        internal set => SetAndRaise(TopBarHeightProperty, ref _topBarHeight, value);
    }

    /// <summary>The height the bottom bars take now.</summary>
    public double BottomBarHeight
    {
        get => _bottomBarHeight;
        internal set => SetAndRaise(BottomBarHeightProperty, ref _bottomBarHeight, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _topBar = e.NameScope.Find<Control>("PART_TopBar");
        _bottomBar = e.NameScope.Find<Control>("PART_BottomBar");
        _topBox = e.NameScope.Find<Control>("PART_TopBox");
        _bottomBox = e.NameScope.Find<Control>("PART_BottomBox");
        _layout = e.NameScope.Find<Control>("PART_Layout");
        UpdateClasses();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopBarStyleProperty || change.Property == BottomBarStyleProperty)
        {
            UpdateClasses();
        }
        else if (change.Property == ExtendContentToTopEdgeProperty || change.Property == ExtendContentToBottomEdgeProperty)
        {
            _layout?.InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // As a window's content the view brings its own bars, as AdwApplicationWindow's: the window's header bar hides,
        // unless the application set it to show.
        if (Parent is Window window)
        {
            _windowBar = window.SetValue(WindowHeaderBar.IsVisibleProperty, false, BindingPriority.Template);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _windowBar?.Dispose();
        _windowBar = null;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ToolbarViewAutomationPeer(this);

    private static string StyleClasses(ToolbarStyle style) => style switch
    {
        ToolbarStyle.Raised => " raised",
        ToolbarStyle.RaisedBorder => " raised border",
        _ => string.Empty,
    };

    // The bars' styles, and update_collapse_style: a box of more than one bar collapses the spacing between them.
    private void UpdateClasses()
    {
        static int Visible(Avalonia.Controls.Controls bars)
        {
            var n = 0;
            foreach (var b in bars)
            {
                n += b.IsVisible ? 1 : 0;
            }

            return n;
        }

        if (_topBar is not null)
        {
            Css.SetClasses(_topBar, "top-bar" + StyleClasses(TopBarStyle));
        }

        if (_bottomBar is not null)
        {
            Css.SetClasses(_bottomBar, "bottom-bar" + StyleClasses(BottomBarStyle));
        }

        if (_topBox is not null)
        {
            Css.SetClasses(_topBox, Visible(TopBars) > 1 ? "vertical collapse-spacing" : "vertical");
        }

        if (_bottomBox is not null)
        {
            Css.SetClasses(_bottomBox, Visible(BottomBars) > 1 ? "vertical collapse-spacing" : "vertical");
        }
    }
}
