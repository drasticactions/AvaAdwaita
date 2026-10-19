using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaAdwaita.Controls;

/// <summary>
/// A row with a title, a subtitle, and widgets before and after them (AdwActionRow). The row's
/// <see cref="ContentControl.Content"/> is shown after <see cref="Suffixes"/>, as a child added to a GTK action row
/// is a suffix.
/// </summary>
public class ActionRow : PreferencesRow
{
    /// <summary>Defines <see cref="Subtitle"/>.</summary>
    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<ActionRow, string?>(nameof(Subtitle));

    /// <summary>Defines <see cref="TitleLines"/>.</summary>
    public static readonly StyledProperty<int> TitleLinesProperty =
        AvaloniaProperty.Register<ActionRow, int>(nameof(TitleLines));

    /// <summary>Defines <see cref="SubtitleLines"/>.</summary>
    public static readonly StyledProperty<int> SubtitleLinesProperty =
        AvaloniaProperty.Register<ActionRow, int>(nameof(SubtitleLines));

    /// <summary>Defines <see cref="HasPrefixes"/>.</summary>
    public static readonly DirectProperty<ActionRow, bool> HasPrefixesProperty =
        AvaloniaProperty.RegisterDirect<ActionRow, bool>(nameof(HasPrefixes), r => r.HasPrefixes);

    /// <summary>Defines <see cref="HasSuffixes"/>.</summary>
    public static readonly DirectProperty<ActionRow, bool> HasSuffixesProperty =
        AvaloniaProperty.RegisterDirect<ActionRow, bool>(nameof(HasSuffixes), r => r.HasSuffixes);

    /// <summary>
    /// The suffixes take the space beyond the title's natural width rather than the title, as a spin row's spin
    /// button does (adw_action_row_set_expand_suffixes).
    /// </summary>
    internal static readonly StyledProperty<bool> ExpandSuffixesProperty =
        AvaloniaProperty.Register<ActionRow, bool>(nameof(ExpandSuffixes));

    internal static readonly DirectProperty<ActionRow, bool> HasSuffixItemsProperty =
        AvaloniaProperty.RegisterDirect<ActionRow, bool>(nameof(HasSuffixItems), r => r.HasSuffixItems);

    private bool _hasPrefixes;
    private bool _hasSuffixes;
    private bool _hasSuffixItems;

    /// <summary>Creates a row.</summary>
    public ActionRow()
    {
        Prefixes.CollectionChanged += OnPartsChanged;
        Suffixes.CollectionChanged += OnPartsChanged;
    }

    /// <summary>The subtitle, under the title in smaller text.</summary>
    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>The most lines the title shows before it ends with an ellipsis; 0 wraps without a limit.</summary>
    public int TitleLines
    {
        get => GetValue(TitleLinesProperty);
        set => SetValue(TitleLinesProperty, value);
    }

    /// <summary>The most lines the subtitle shows before it ends with an ellipsis; 0 wraps without a limit.</summary>
    public int SubtitleLines
    {
        get => GetValue(SubtitleLinesProperty);
        set => SetValue(SubtitleLinesProperty, value);
    }

    /// <summary>Widgets before the title, as an icon or a check button.</summary>
    public Avalonia.Controls.Controls Prefixes { get; } = new();

    /// <summary>Widgets after the title, as buttons or a switch.</summary>
    public Avalonia.Controls.Controls Suffixes { get; } = new();

    /// <summary>Whether <see cref="Prefixes"/> has anything to show.</summary>
    public bool HasPrefixes
    {
        get => _hasPrefixes;
        private set => SetAndRaise(HasPrefixesProperty, ref _hasPrefixes, value);
    }

    /// <summary>Whether there is a suffix to show: <see cref="Suffixes"/> or the content.</summary>
    public bool HasSuffixes
    {
        get => _hasSuffixes;
        private set => SetAndRaise(HasSuffixesProperty, ref _hasSuffixes, value);
    }

    internal bool ExpandSuffixes
    {
        get => GetValue(ExpandSuffixesProperty);
        set => SetValue(ExpandSuffixesProperty, value);
    }

    internal bool HasSuffixItems
    {
        get => _hasSuffixItems;
        private set => SetAndRaise(HasSuffixItemsProperty, ref _hasSuffixItems, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            UpdateParts();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ActionRowAutomationPeer(this);

    // The template shows the parts through items controls, which become their logical parents.
    private void OnPartsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateParts();

    private void UpdateParts()
    {
        HasPrefixes = Prefixes.Count > 0;
        HasSuffixItems = Suffixes.Count > 0;
        HasSuffixes = HasSuffixItems || Content is not null;
    }
}
