using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A page of a <see cref="TabView"/> (AdwTabPage): its <see cref="Content"/> and what its tab shows, its title, icon,
/// indicator and attention.
/// </summary>
public class TabPage : AvaloniaObject
{
    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<TabPage, Control?>(nameof(Content));

    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<TabPage, string?>(nameof(Title));

    /// <summary>Defines <see cref="Tooltip"/>.</summary>
    public static readonly StyledProperty<string?> TooltipProperty =
        AvaloniaProperty.Register<TabPage, string?>(nameof(Tooltip));

    /// <summary>Defines <see cref="IconName"/>.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<TabPage, string?>(nameof(IconName));

    /// <summary>Defines <see cref="IsLoading"/>.</summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<TabPage, bool>(nameof(IsLoading));

    /// <summary>Defines <see cref="IndicatorIconName"/>.</summary>
    public static readonly StyledProperty<string?> IndicatorIconNameProperty =
        AvaloniaProperty.Register<TabPage, string?>(nameof(IndicatorIconName));

    /// <summary>Defines <see cref="IndicatorTooltip"/>.</summary>
    public static readonly StyledProperty<string?> IndicatorTooltipProperty =
        AvaloniaProperty.Register<TabPage, string?>(nameof(IndicatorTooltip));

    /// <summary>Defines <see cref="IsIndicatorActivatable"/>.</summary>
    public static readonly StyledProperty<bool> IsIndicatorActivatableProperty =
        AvaloniaProperty.Register<TabPage, bool>(nameof(IsIndicatorActivatable));

    /// <summary>Defines <see cref="NeedsAttention"/>.</summary>
    public static readonly StyledProperty<bool> NeedsAttentionProperty =
        AvaloniaProperty.Register<TabPage, bool>(nameof(NeedsAttention));

    /// <summary>Defines <see cref="Keyword"/>.</summary>
    public static readonly StyledProperty<string?> KeywordProperty =
        AvaloniaProperty.Register<TabPage, string?>(nameof(Keyword));

    /// <summary>Defines <see cref="ThumbnailXAlign"/>.</summary>
    public static readonly StyledProperty<double> ThumbnailXAlignProperty =
        AvaloniaProperty.Register<TabPage, double>(nameof(ThumbnailXAlign));

    /// <summary>Defines <see cref="ThumbnailYAlign"/>.</summary>
    public static readonly StyledProperty<double> ThumbnailYAlignProperty =
        AvaloniaProperty.Register<TabPage, double>(nameof(ThumbnailYAlign));

    /// <summary>Defines <see cref="LiveThumbnail"/>.</summary>
    public static readonly StyledProperty<bool> LiveThumbnailProperty =
        AvaloniaProperty.Register<TabPage, bool>(nameof(LiveThumbnail));

    /// <summary>Defines <see cref="IsSelected"/>.</summary>
    public static readonly DirectProperty<TabPage, bool> IsSelectedProperty =
        AvaloniaProperty.RegisterDirect<TabPage, bool>(nameof(IsSelected), p => p.IsSelected);

    /// <summary>Defines <see cref="IsPinned"/>.</summary>
    public static readonly DirectProperty<TabPage, bool> IsPinnedProperty =
        AvaloniaProperty.RegisterDirect<TabPage, bool>(nameof(IsPinned), p => p.IsPinned);

    /// <summary>Defines <see cref="Parent"/>.</summary>
    public static readonly DirectProperty<TabPage, TabPage?> ParentProperty =
        AvaloniaProperty.RegisterDirect<TabPage, TabPage?>(nameof(Parent), p => p.Parent);

    private bool _isSelected;
    private bool _isPinned;
    private TabPage? _parent;

    /// <summary>Creates a page.</summary>
    public TabPage()
    {
        Css.SetNode(Bin, "widget");
    }

    /// <summary>Creates a page holding <paramref name="content"/>.</summary>
    public TabPage(Control content)
        : this()
    {
        Content = content;
    }

    /// <summary>The page's content, shown while the page is selected.</summary>
    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The page's title, on its tab.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The tab's tooltip; the title when unset.</summary>
    public string? Tooltip
    {
        get => GetValue(TooltipProperty);
        set => SetValue(TooltipProperty, value);
    }

    /// <summary>The page's icon, before the title; a pinned page without one shows the view's default icon.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>Whether the page is loading: a spinner takes the icon's place.</summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>An icon at the start of the tab, as a muted speaker.</summary>
    public string? IndicatorIconName
    {
        get => GetValue(IndicatorIconNameProperty);
        set => SetValue(IndicatorIconNameProperty, value);
    }

    /// <summary>The indicator's tooltip.</summary>
    public string? IndicatorTooltip
    {
        get => GetValue(IndicatorTooltipProperty);
        set => SetValue(IndicatorTooltipProperty, value);
    }

    /// <summary>Whether clicking the indicator raises <see cref="TabView.IndicatorActivated"/>.</summary>
    public bool IsIndicatorActivatable
    {
        get => GetValue(IsIndicatorActivatableProperty);
        set => SetValue(IsIndicatorActivatableProperty, value);
    }

    /// <summary>Whether the page wants attention: its tab is marked, and so is the edge of a tab bar it is scrolled past.</summary>
    public bool NeedsAttention
    {
        get => GetValue(NeedsAttentionProperty);
        set => SetValue(NeedsAttentionProperty, value);
    }

    /// <summary>A word a tab overview's search finds the page by, besides its title.</summary>
    public string? Keyword
    {
        get => GetValue(KeywordProperty);
        set => SetValue(KeywordProperty, value);
    }

    /// <summary>Where a tab overview's thumbnail crops the page horizontally, 0 to 1.</summary>
    public double ThumbnailXAlign
    {
        get => GetValue(ThumbnailXAlignProperty);
        set => SetValue(ThumbnailXAlignProperty, value);
    }

    /// <summary>Where a tab overview's thumbnail crops the page vertically, 0 to 1.</summary>
    public double ThumbnailYAlign
    {
        get => GetValue(ThumbnailYAlignProperty);
        set => SetValue(ThumbnailYAlignProperty, value);
    }

    /// <summary>Whether a tab overview's thumbnail of the page follows it as it changes.</summary>
    public bool LiveThumbnail
    {
        get => GetValue(LiveThumbnailProperty);
        set => SetValue(LiveThumbnailProperty, value);
    }

    /// <summary>Whether the page is its view's selected page.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        internal set => SetAndRaise(IsSelectedProperty, ref _isSelected, value);
    }

    /// <summary>Whether the page is pinned (<see cref="TabView.SetPagePinned"/>).</summary>
    public bool IsPinned
    {
        get => _isPinned;
        internal set => SetAndRaise(IsPinnedProperty, ref _isPinned, value);
    }

    /// <summary>The page this page was opened from (<see cref="TabView.AddPage"/>), which closing it goes back to.</summary>
    public TabPage? Parent
    {
        get => _parent;
        internal set => SetAndRaise(ParentProperty, ref _parent, value);
    }

    /// <summary>The page's bin in its view (AdwTabPage's bin): a <c>widget</c> node holding the content.</summary>
    internal Primitives.TabPageBin Bin { get; } = new();

    /// <summary>Whether closing the page has been asked and not finished.</summary>
    internal bool IsClosing { get; set; }

    /// <summary>The control focused in the page when it was last deselected.</summary>
    internal Avalonia.Input.IInputElement? LastFocus { get; set; }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            Bin.Child = Content;
        }
    }
}
