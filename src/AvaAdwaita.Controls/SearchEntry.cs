using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// An entry for search text (GtkSearchEntry): a search icon before the text and a clear icon after it while there is
/// text. <see cref="SearchChanged"/> is raised a moment after the text stops changing, <see cref="Activated"/> on
/// Enter, and <see cref="StopSearch"/> on Escape.
/// </summary>
public class SearchEntry : TextBox
{
    /// <summary>Defines <see cref="SearchDelay"/>.</summary>
    public static readonly StyledProperty<int> SearchDelayProperty =
        AvaloniaProperty.Register<SearchEntry, int>(nameof(SearchDelay), 150);

    /// <summary>Defines <see cref="SearchChanged"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> SearchChangedEvent =
        RoutedEvent.Register<SearchEntry, RoutedEventArgs>(nameof(SearchChanged), RoutingStrategies.Bubble);

    /// <summary>Defines <see cref="Activated"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ActivatedEvent =
        RoutedEvent.Register<SearchEntry, RoutedEventArgs>(nameof(Activated), RoutingStrategies.Bubble);

    /// <summary>Defines <see cref="StopSearch"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> StopSearchEvent =
        RoutedEvent.Register<SearchEntry, RoutedEventArgs>(nameof(StopSearch), RoutingStrategies.Bubble);

    private readonly AdwIcon _searchIcon = new() { IconName = "system-search-symbolic", IsHitTestVisible = false };
    private readonly AdwIcon _clearIcon = new() { IconName = "edit-clear-symbolic", IsVisible = false, Cursor = new Cursor(StandardCursorType.Arrow) };
    private DispatcherTimer? _delay;

    static SearchEntry()
    {
        CssNodeProviders.Register<SearchEntry>(_ => ["search"]);
    }

    /// <summary>Creates a search entry: an <c>entry.search</c> node with its icons.</summary>
    public SearchEntry()
    {
        Css.SetNode(_searchIcon, "image");
        Css.SetNode(_clearIcon, "image");
        _clearIcon[!ToolTip.TipProperty] = _clearIcon.GetResourceObservable("AdwStringClearEntry").ToBinding();
        // The icon takes the press, which the text would otherwise capture, and clears on the release over it.
        _clearIcon.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(_clearIcon).Properties.IsLeftButtonPressed)
            {
                e.Handled = true;
                e.Pointer.Capture(_clearIcon);
            }
        };
        _clearIcon.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            e.Pointer.Capture(null);
            if (new Rect(_clearIcon.Bounds.Size).Contains(e.GetPosition(_clearIcon)))
            {
                Clear();
            }
        };
        InnerLeftContent = _searchIcon;
        InnerRightContent = _clearIcon;
    }

    /// <summary>Raised a moment (<see cref="SearchDelay"/>) after the text stops changing, and at once when it is cleared.</summary>
    public event EventHandler<RoutedEventArgs>? SearchChanged
    {
        add => AddHandler(SearchChangedEvent, value);
        remove => RemoveHandler(SearchChangedEvent, value);
    }

    /// <summary>Raised on Enter.</summary>
    public event EventHandler<RoutedEventArgs>? Activated
    {
        add => AddHandler(ActivatedEvent, value);
        remove => RemoveHandler(ActivatedEvent, value);
    }

    /// <summary>Raised on Escape, to end the search.</summary>
    public event EventHandler<RoutedEventArgs>? StopSearch
    {
        add => AddHandler(StopSearchEvent, value);
        remove => RemoveHandler(StopSearchEvent, value);
    }

    /// <summary>How long after the text stops changing <see cref="SearchChanged"/> is raised, in milliseconds.</summary>
    public int SearchDelay
    {
        get => GetValue(SearchDelayProperty);
        set => SetValue(SearchDelayProperty, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.SearchEntryAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            var empty = string.IsNullOrEmpty(Text);
            _clearIcon.IsVisible = !empty;

            // gtk_search_entry_changed: an emptied entry searches at once; otherwise after the delay.
            _delay?.Stop();
            if (empty)
            {
                RaiseEvent(new RoutedEventArgs(SearchChangedEvent, this));
            }
            else
            {
                _delay ??= new DispatcherTimer(TimeSpan.FromMilliseconds(SearchDelay), DispatcherPriority.Normal, (_, _) =>
                {
                    _delay!.Stop();
                    RaiseEvent(new RoutedEventArgs(SearchChangedEvent, this));
                });
                _delay.Interval = TimeSpan.FromMilliseconds(SearchDelay);
                _delay.Start();
            }
        }
        else if (change.Property == Css.StyleProperty && Css.GetStyle(this) is { } style)
        {
            // The entry's border-spacing between the icons and the text.
            var spacing = style.Size.BorderSpacing.X.Value;
            _searchIcon.Margin = new Thickness(0, 0, spacing, 0);
            _clearIcon.Margin = new Thickness(spacing, 0, 0, 0);
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !AcceptsReturn)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(ActivatedEvent, this));
            return;
        }

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(StopSearchEvent, this));
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // gtk_search_entry_measure counts the clear icon whether or not it shows.
        var size = base.MeasureOverride(availableSize);
        if (!_clearIcon.IsVisible)
        {
            size = size.WithWidth(size.Width + _clearIcon.Margin.Left + _clearIcon.IconSize);
        }

        return size;
    }
}
