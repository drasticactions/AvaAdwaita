using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// Linked toggle buttons for the pages of a <see cref="ViewStack"/> (GtkStackSwitcher): each shows its page's icon, or
/// its title when it has none. The buttons share the switcher's width.
/// </summary>
public class StackSwitcher : TemplatedControl
{
    /// <summary>Defines <see cref="Stack"/>.</summary>
    public static readonly StyledProperty<ViewStack?> StackProperty =
        ViewSwitcher.StackProperty.AddOwner<StackSwitcher>();

    private Panel? _panel;
    private bool _syncing;

    /// <summary>The stack whose pages the switcher shows.</summary>
    public ViewStack? Stack
    {
        get => GetValue(StackProperty);
        set => SetValue(StackProperty, value);
    }

    /// <summary>The buttons, one per page.</summary>
    internal Avalonia.Controls.Controls Buttons => _panel?.Children ?? new();

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<Panel>("PART_Buttons");
        Rebuild();
    }

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
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.StackSwitcherAutomationPeer(this);

    private void OnPagesChanged(object? sender, System.EventArgs e) => Rebuild();

    private void OnStackPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ViewStack.VisiblePageProperty)
        {
            UpdateChecked();
        }
    }

    // rebuild_child and update_button in gtkstackswitcher.c: the icon if the page has one, else its title.
    private void Rebuild()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Children.Clear();
        foreach (var page in Stack?.Pages ?? [])
        {
            var button = new ToggleButton
            {
                Tag = page,
                Content = page.IconName is { } icon ? new AdwIcon { IconName = icon } : page.Title,

                // The child keeps its natural width in the middle of the wider button.
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                IsVisible = page.IsPageVisible && (page.Title is not null || page.IconName is not null),
            };
            button.Classes.Set("needs-attention", page.NeedsAttention);
            AdwStack.SetExpand(button, true);
            button.IsCheckedChanged += OnButtonChecked;
            _panel.Children.Add(button);
        }

        UpdateChecked();
    }

    private void UpdateChecked()
    {
        _syncing = true;
        try
        {
            foreach (var c in _panel?.Children ?? [])
            {
                if (c is ToggleButton b)
                {
                    b.IsChecked = b.Tag == Stack?.VisiblePage;
                }
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnButtonChecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        if (sender is ToggleButton { IsChecked: true, Tag: ViewStackPage page } && Stack is { } stack)
        {
            stack.VisiblePage = page;
        }

        UpdateChecked();
    }
}
