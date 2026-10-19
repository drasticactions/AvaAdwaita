using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A bar that slides down to hold a search entry (GtkSearchBar). Turning <see cref="IsSearchMode"/> on reveals it and
/// focuses the entry; Escape in the entry, or the close button, hides it and clears the entry. Typing in
/// <see cref="KeyCaptureTarget"/> starts a search with what was typed.
/// </summary>
public class SearchBar : Control
{
    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<SearchBar, Control?>(nameof(Content));

    /// <summary>Defines <see cref="IsSearchMode"/>.</summary>
    public static readonly StyledProperty<bool> IsSearchModeProperty =
        AvaloniaProperty.Register<SearchBar, bool>(nameof(IsSearchMode), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="ShowCloseButton"/>.</summary>
    public static readonly StyledProperty<bool> ShowCloseButtonProperty =
        AvaloniaProperty.Register<SearchBar, bool>(nameof(ShowCloseButton));

    /// <summary>Defines <see cref="Entry"/>.</summary>
    public static readonly StyledProperty<TextBox?> EntryProperty =
        AvaloniaProperty.Register<SearchBar, TextBox?>(nameof(Entry));

    /// <summary>Defines <see cref="KeyCaptureTarget"/>.</summary>
    public static readonly StyledProperty<InputElement?> KeyCaptureTargetProperty =
        AvaloniaProperty.Register<SearchBar, InputElement?>(nameof(KeyCaptureTarget));

    private readonly Revealer _revealer = new();
    private readonly AdwBox _box = new();
    private readonly ContentPresenter _center = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Button _close = new() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
    private TextBox? _connected;
    private InputElement? _capturing;

    /// <summary>Creates a search bar: a <c>searchbar</c> node holding <c>revealer &gt; box</c>.</summary>
    public SearchBar()
    {
        SetValue(Css.NodeProperty, "searchbar", BindingPriority.Style);
        Css.SetNode(_box, "box");
        var close = new AdwIcon { IconName = "window-close-symbolic" };
        Css.SetNode(close, "image");
        _close.Content = close;
        _close.Classes.Add("close");
        _close.IsVisible = false;
        _close.Click += (_, _) => SetCurrentValue(IsSearchModeProperty, false);
        var grid = new CenterGrid();
        Grid.SetColumn(_center, 1);
        Grid.SetColumn(_close, 2);
        grid.Children.Add(_center);
        grid.Children.Add(_close);
        _box.Child = grid;
        _revealer.Child = _box;
        _revealer.PropertyChanged += (_, e) =>
        {
            if (e.Property == Revealer.IsChildRevealedProperty)
            {
                OnRevealed(_revealer.IsChildRevealed);
            }
        };
        VisualChildren.Add(_revealer);
        LogicalChildren.Add(_revealer);
    }

    /// <summary>The bar's content, centered: a search entry, or a box with one.</summary>
    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Whether the bar shows, searching.</summary>
    public bool IsSearchMode
    {
        get => GetValue(IsSearchModeProperty);
        set => SetValue(IsSearchModeProperty, value);
    }

    /// <summary>Whether a close button shows at the end.</summary>
    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>The entry the bar searches with (gtk_search_bar_connect_entry); the first text box in the content when unset.</summary>
    public TextBox? Entry
    {
        get => GetValue(EntryProperty);
        set => SetValue(EntryProperty, value);
    }

    /// <summary>Where typing starts a search: text typed there while the bar is hidden goes to the entry.</summary>
    public InputElement? KeyCaptureTarget
    {
        get => GetValue(KeyCaptureTargetProperty);
        set => SetValue(KeyCaptureTargetProperty, value);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.SearchBarAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(ConnectEntry, DispatcherPriority.Loaded);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            _center.Content = Content;
            Dispatcher.UIThread.Post(ConnectEntry, DispatcherPriority.Loaded);
        }
        else if (change.Property == EntryProperty)
        {
            ConnectEntry();
        }
        else if (change.Property == IsSearchModeProperty)
        {
            _revealer.IsRevealed = IsSearchMode;
        }
        else if (change.Property == ShowCloseButtonProperty)
        {
            _close.IsVisible = ShowCloseButton;
        }
        else if (change.Property == KeyCaptureTargetProperty)
        {
            if (_capturing is not null)
            {
                _capturing.RemoveHandler(TextInputEvent, OnCapturedText);
            }

            _capturing = KeyCaptureTarget;
            _capturing?.AddHandler(TextInputEvent, OnCapturedText, RoutingStrategies.Tunnel);
        }
    }

    private void ConnectEntry()
    {
        var entry = Entry ?? (Content as TextBox) ?? Content?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        if (ReferenceEquals(entry, _connected))
        {
            return;
        }

        if (_connected is not null)
        {
            _connected.RemoveHandler(SearchEntry.StopSearchEvent, OnStopSearch);
            _connected.RemoveHandler(KeyDownEvent, OnEntryKeyDown);
        }

        _connected = entry;
        if (entry is SearchEntry)
        {
            entry.AddHandler(SearchEntry.StopSearchEvent, OnStopSearch);
        }
        else
        {
            entry?.AddHandler(KeyDownEvent, OnEntryKeyDown, RoutingStrategies.Tunnel);
        }
    }

    // reveal_child_changed_cb: revealed, the entry takes the focus; hidden, it is cleared.
    private void OnRevealed(bool revealed)
    {
        if (_connected is not { } entry)
        {
            return;
        }

        if (revealed)
        {
            // Once the revealer shows its child, which can take the focus then.
            Dispatcher.UIThread.Post(
                () =>
                {
                    entry.Focus();
                    entry.CaretIndex = entry.Text?.Length ?? 0;
                },
                DispatcherPriority.Loaded);
        }
        else
        {
            entry.Text = string.Empty;
        }
    }

    private void OnStopSearch(object? sender, RoutedEventArgs e) => SetCurrentValue(IsSearchModeProperty, false);

    private void OnEntryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            SetCurrentValue(IsSearchModeProperty, false);
        }
    }

    // gtk_search_bar_handle_event and the entry's key capture: text typed outside the entry opens the bar and goes to
    // the entry, also while the bar is still opening and the entry cannot take the focus yet.
    private void OnCapturedText(object? sender, TextInputEventArgs e)
    {
        if (_connected is not { } entry || string.IsNullOrEmpty(e.Text) || e.Source is TextBox || char.IsControl(e.Text[0]))
        {
            return;
        }

        e.Handled = true;
        entry.Text = (entry.Text ?? string.Empty) + e.Text;
        SetCurrentValue(IsSearchModeProperty, true);
        entry.Focus();
        entry.CaretIndex = entry.Text.Length;
    }
}
