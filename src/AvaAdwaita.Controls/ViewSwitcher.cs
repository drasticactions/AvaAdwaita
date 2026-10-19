using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls;

/// <summary>
/// Buttons of the same width for the pages of a <see cref="ViewStack"/> (AdwViewSwitcher), usually a header bar's title
/// widget. <see cref="Policy"/> puts each icon beside its title or over it.
/// </summary>
public class ViewSwitcher : TemplatedControl
{
    /// <summary>Defines <see cref="Stack"/>.</summary>
    public static readonly StyledProperty<ViewStack?> StackProperty =
        AvaloniaProperty.Register<ViewSwitcher, ViewStack?>(nameof(Stack));

    /// <summary>Defines <see cref="Policy"/>.</summary>
    public static readonly StyledProperty<ViewSwitcherPolicy> PolicyProperty =
        AvaloniaProperty.Register<ViewSwitcher, ViewSwitcherPolicy>(nameof(Policy));

    private readonly Avalonia.Controls.Controls _buttons = new();
    private Panel? _panel;

    static ViewSwitcher()
    {
        AffectsMeasure<ViewSwitcher>(PolicyProperty);
        CssNodeProviders.Register<ViewSwitcher>(s => [s.Policy == ViewSwitcherPolicy.Wide ? "wide" : "narrow"], PolicyProperty);
    }

    /// <summary>The stack whose pages the switcher shows.</summary>
    public ViewStack? Stack
    {
        get => GetValue(StackProperty);
        set => SetValue(StackProperty, value);
    }

    /// <summary>Whether each icon is beside its title or over it.</summary>
    public ViewSwitcherPolicy Policy
    {
        get => GetValue(PolicyProperty);
        set => SetValue(PolicyProperty, value);
    }

    /// <summary>The buttons, one per page.</summary>
    internal Avalonia.Controls.Controls Buttons => _buttons;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StackProperty)
        {
            if (change.OldValue is ViewStack old)
            {
                old.PagesChanged -= OnPagesChanged;
                old.PropertyChanged -= OnStackPropertyChanged;
            }

            if (change.NewValue is ViewStack stack)
            {
                stack.PagesChanged += OnPagesChanged;
                stack.PropertyChanged += OnStackPropertyChanged;
            }

            Rebuild();
        }
        else if (change.Property == PolicyProperty)
        {
            foreach (ViewSwitcherButton b in _buttons)
            {
                b.Orientation = Policy == ViewSwitcherPolicy.Wide ? Orientation.Horizontal : Orientation.Vertical;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _panel?.Children.Clear();
        _panel = e.NameScope.Find<Panel>("PART_Buttons");
        _panel?.Children.AddRange(_buttons);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ViewSwitcherAutomationPeer(this);

    private void OnPagesChanged(object? sender, System.EventArgs e) => Rebuild();

    private void OnStackPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ViewStack.VisiblePageProperty)
        {
            UpdateChecked();
        }
    }

    private void Rebuild()
    {
        foreach (ViewSwitcherButton b in _buttons)
        {
            b.IsCheckedChanged -= OnButtonChecked;
        }

        _panel?.Children.Clear();
        _buttons.Clear();
        foreach (var page in Stack?.Pages ?? [])
        {
            var button = new ViewSwitcherButton
            {
                Page = page,
                Orientation = Policy == ViewSwitcherPolicy.Wide ? Orientation.Horizontal : Orientation.Vertical,
                IsVisible = page.IsPageVisible && (page.Title is not null || page.IconName is not null),
            };
            button.IsCheckedChanged += OnButtonChecked;
            _buttons.Add(button);
        }

        _panel?.Children.AddRange(_buttons);

        UpdateChecked();
        InvalidateMeasure();
    }

    private void UpdateChecked()
    {
        foreach (ViewSwitcherButton b in _buttons)
        {
            b.IsChecked = b.Page == Stack?.VisiblePage;
        }
    }

    private void OnButtonChecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is ViewSwitcherButton { IsChecked: true, Page: { } page } && Stack is { } stack)
        {
            stack.VisiblePage = page;
        }

        // A page's button stays checked while its page shows; clicking it again does not uncheck it.
        UpdateChecked();
    }
}
