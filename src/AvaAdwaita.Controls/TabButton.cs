using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A button showing how many pages a <see cref="TabView"/> has (AdwTabButton), marked when a page other than the selected
/// one needs attention; usually it opens a <see cref="TabOverview"/>. From 100 pages it shows an infinity sign.
/// </summary>
public class TabButton : Control
{
    /// <summary>Defines <see cref="View"/>.</summary>
    public static readonly StyledProperty<TabView?> ViewProperty =
        AvaloniaProperty.Register<TabButton, TabView?>(nameof(View));

    /// <summary>Defines <see cref="Command"/>.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        Button.CommandProperty.AddOwner<TabButton>();

    /// <summary>Defines <see cref="CommandParameter"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        Button.CommandParameterProperty.AddOwner<TabButton>();

    /// <summary>Defines <see cref="Click"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<TabButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private readonly Button _button = new() { Classes = { "image-button" } };
    private readonly IndicatorBin _indicator = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly AdwIcon _icon = new() { IconName = "adw-tab-counter-symbolic" };
    private readonly AdwLabel _label = new() { HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = Avalonia.Media.TextAlignment.Center };
    private TabView? _view;

    /// <summary>Creates a tab button: a <c>tabbutton</c> node holding <c>button.image-button</c>.</summary>
    public TabButton()
    {
        SetValue(Css.NodeProperty, "tabbutton", BindingPriority.Style);
        Css.SetNode(_icon, "image");
        Css.SetNode(_label, "label");
        var overlay = new AdwBox { Child = new Panel { Children = { _icon, _label } } };
        Css.SetNode(overlay, "overlay");
        _indicator.Child = overlay;
        _button.Content = _indicator;
        _button[!ToolTip.TipProperty] = _button.GetResourceObservable("AdwStringViewOpenTabs").ToBinding();
        _button.Bind(Button.CommandProperty, this.GetObservable(CommandProperty));
        _button.Bind(Button.CommandParameterProperty, this.GetObservable(CommandParameterProperty));
        _button.Click += (_, _) => RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        VisualChildren.Add(_button);
        LogicalChildren.Add(_button);
        UpdateIcon();
    }

    /// <summary>Raised when the button is clicked.</summary>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>The view whose pages the button counts.</summary>
    public TabView? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    /// <summary>What clicking the button does, as opening a tab overview.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The command's parameter.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>The button's tooltip, in the user's language.</summary>
    internal string? Tip => ToolTip.GetTip(_button) as string;

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.TabButtonAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewProperty)
        {
            if (_view is { } old)
            {
                old.PropertyChanged -= OnViewPropertyChanged;
                old.PageAttached -= OnPageAttached;
                old.PageDetached -= OnPageDetached;
                foreach (var page in old.Pages)
                {
                    page.PropertyChanged -= OnPagePropertyChanged;
                }
            }

            _view = View;
            if (_view is { } view)
            {
                view.PropertyChanged += OnViewPropertyChanged;
                view.PageAttached += OnPageAttached;
                view.PageDetached += OnPageDetached;
                foreach (var page in view.Pages)
                {
                    page.PropertyChanged += OnPagePropertyChanged;
                }
            }

            UpdateIcon();
            UpdateNeedsAttention();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _button.Measure(availableSize);
        return _button.DesiredSize;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        _button.Arrange(new Rect(finalSize));
        return finalSize;
    }

    // update_icon: the count on the icon, smaller from 10, and the overflow icon from 100.
    private void UpdateIcon()
    {
        var count = _view?.PageCount;
        var display = count is < 100;
        Css.SetClasses(_label, count >= 10 ? "numeric small" : "numeric");
        _label.IsVisible = display;
        _label.Text = display ? count!.Value.ToString(System.Globalization.CultureInfo.CurrentCulture) : null;
        _icon.IconName = count >= 100 ? "adw-tab-overflow-symbolic" : "adw-tab-counter-symbolic";
    }

    // update_needs_attention: a page other than the selected one wants attention.
    private void UpdateNeedsAttention() =>
        _indicator.NeedsAttention = _view?.Pages.Any(p => !p.IsSelected && p.NeedsAttention) == true;

    private void OnViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TabView.PageCountProperty)
        {
            UpdateIcon();
        }
        else if (e.Property == TabView.SelectedPageProperty)
        {
            UpdateNeedsAttention();
        }
    }

    private void OnPageAttached(object? sender, TabPageEventArgs e)
    {
        if (e.Page is { } page)
        {
            page.PropertyChanged += OnPagePropertyChanged;
        }

        UpdateNeedsAttention();
    }

    private void OnPageDetached(object? sender, TabPageEventArgs e)
    {
        if (e.Page is { } page)
        {
            page.PropertyChanged -= OnPagePropertyChanged;
        }

        UpdateNeedsAttention();
    }

    private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TabPage.NeedsAttentionProperty || e.Property == TabPage.IsSelectedProperty)
        {
            UpdateNeedsAttention();
        }
    }
}
