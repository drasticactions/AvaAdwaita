using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Metadata;

namespace AvaAdwaita.Controls;

/// <summary>
/// Pages of which one shows at a time (AdwViewStack), each with the title, icon and badge a <see cref="ViewSwitcher"/>,
/// a view switcher bar or an inline view switcher shows for it.
/// </summary>
public class ViewStack : TemplatedControl
{
    /// <summary>Defines <see cref="VisiblePage"/>.</summary>
    public static readonly StyledProperty<ViewStackPage?> VisiblePageProperty =
        AvaloniaProperty.Register<ViewStack, ViewStackPage?>(nameof(VisiblePage), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="VisiblePageName"/>.</summary>
    public static readonly StyledProperty<string?> VisiblePageNameProperty =
        AvaloniaProperty.Register<ViewStack, string?>(nameof(VisiblePageName), defaultBindingMode: BindingMode.TwoWay);

    private bool _syncing;

    /// <summary>Creates a view stack.</summary>
    public ViewStack()
    {
        Pages.CollectionChanged += OnPagesChanged;
    }

    /// <summary>Raised when the pages change, or a page's title, icon, badge, attention or visibility does.</summary>
    public event EventHandler? PagesChanged;

    /// <summary>The pages.</summary>
    [Content]
    public AvaloniaList<ViewStackPage> Pages { get; } = new();

    /// <summary>The page that shows; the first visible page while none is set.</summary>
    public ViewStackPage? VisiblePage
    {
        get => GetValue(VisiblePageProperty);
        set => SetValue(VisiblePageProperty, value);
    }

    /// <summary>The name of the page that shows.</summary>
    public string? VisiblePageName
    {
        get => GetValue(VisiblePageNameProperty);
        set => SetValue(VisiblePageNameProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_syncing)
        {
            return;
        }

        if (change.Property == VisiblePageProperty)
        {
            Show(change.GetNewValue<ViewStackPage?>());
        }
        else if (change.Property == VisiblePageNameProperty && Pages.FirstOrDefault(p => p.Name == change.GetNewValue<string?>()) is { } named)
        {
            Show(named);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ViewStackAutomationPeer(this);

    private void OnPagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (ViewStackPage page in e.OldItems ?? Array.Empty<ViewStackPage>())
        {
            page.PropertyChanged -= OnPagePropertyChanged;
        }

        foreach (ViewStackPage page in e.NewItems ?? Array.Empty<ViewStackPage>())
        {
            page.PropertyChanged += OnPagePropertyChanged;
        }

        if (VisiblePage is null || !Pages.Contains(VisiblePage))
        {
            Show(Pages.FirstOrDefault(p => p.IsPageVisible));
        }
        else
        {
            Show(VisiblePage);
        }

        PagesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ViewStackPage.TitleProperty || e.Property == ViewStackPage.IconNameProperty || e.Property == ViewStackPage.BadgeNumberProperty
            || e.Property == ViewStackPage.NeedsAttentionProperty || e.Property == ViewStackPage.IsPageVisibleProperty
            || e.Property == ViewStackPage.UseUnderlineProperty || e.Property == NameProperty)
        {
            PagesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Show(ViewStackPage? page)
    {
        _syncing = true;
        try
        {
            VisiblePage = page;
            VisiblePageName = page?.Name;
        }
        finally
        {
            _syncing = false;
        }

        // Only the visible page is in the stack's node, as GtkStack maps only its visible child.
        foreach (var p in Pages)
        {
            p.IsVisible = p == page;
        }
    }
}
