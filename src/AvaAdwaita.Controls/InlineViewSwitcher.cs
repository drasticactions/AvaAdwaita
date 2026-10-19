using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A toggle group for the pages of a <see cref="ViewStack"/> (AdwInlineViewSwitcher), for a view inside a window rather
/// than its header bar. <see cref="DisplayMode"/> shows each page's title, icon or both.
/// </summary>
public class InlineViewSwitcher : TemplatedControl
{
    /// <summary>Defines <see cref="Stack"/>.</summary>
    public static readonly StyledProperty<ViewStack?> StackProperty =
        ViewSwitcher.StackProperty.AddOwner<InlineViewSwitcher>();

    /// <summary>Defines <see cref="DisplayMode"/>.</summary>
    public static readonly StyledProperty<InlineViewSwitcherDisplayMode> DisplayModeProperty =
        AvaloniaProperty.Register<InlineViewSwitcher, InlineViewSwitcherDisplayMode>(nameof(DisplayMode));

    /// <summary>Defines <see cref="CanShrink"/>.</summary>
    public static readonly StyledProperty<bool> CanShrinkProperty =
        AvaloniaProperty.Register<InlineViewSwitcher, bool>(nameof(CanShrink), true);

    private InlineToggleList? _list;
    private bool _syncing;

    /// <summary>The stack whose pages the switcher shows.</summary>
    public ViewStack? Stack
    {
        get => GetValue(StackProperty);
        set => SetValue(StackProperty, value);
    }

    /// <summary>What the toggles show.</summary>
    public InlineViewSwitcherDisplayMode DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    /// <summary>Lets the titles end with an ellipsis when the toggles do not fit.</summary>
    public bool CanShrink
    {
        get => GetValue(CanShrinkProperty);
        set => SetValue(CanShrinkProperty, value);
    }

    /// <summary>The toggle group.</summary>
    internal InlineToggleList? List => _list;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_list is not null)
        {
            _list.SelectionChanged -= OnSelectionChanged;
        }

        _list = e.NameScope.Find<InlineToggleList>("PART_Toggles");
        if (_list is not null)
        {
            _list.SelectionChanged += OnSelectionChanged;
        }

        Update();
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

            Update();
        }
        else if (change.Property == DisplayModeProperty || change.Property == CanShrinkProperty)
        {
            Update();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.InlineViewSwitcherAutomationPeer(this);

    private void OnPagesChanged(object? sender, System.EventArgs e) => Update();

    private void OnStackPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ViewStack.VisiblePageProperty)
        {
            Select();
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_syncing && _list?.SelectedItem is PageItem { Page: var page } && Stack is { } stack)
        {
            stack.VisiblePage = page;
        }
    }

    private void Update()
    {
        if (_list is null)
        {
            return;
        }

        Css.SetClasses(_list, DisplayMode switch
        {
            InlineViewSwitcherDisplayMode.Icons => "icons",
            InlineViewSwitcherDisplayMode.Both => "both",
            _ => "labels",
        });
        _syncing = true;
        try
        {
            // The pages are controls of the stack, so the toggles show records of them rather than the pages themselves.
            _list.ItemTemplate = new FuncDataTemplate<PageItem>((item, _) => Toggle(item?.Page), supportsRecycling: false);
            _list.ItemsSource = Stack?.Pages.Where(p => p.IsPageVisible).Select(p => new PageItem(p)).ToList();
        }
        finally
        {
            _syncing = false;
        }

        Select();
    }

    private void Select()
    {
        if (_list is null)
        {
            return;
        }

        _syncing = true;
        try
        {
            _list.SelectedItem = _list.ItemsSource?.OfType<PageItem>().FirstOrDefault(i => i.Page == Stack?.VisiblePage);
        }
        finally
        {
            _syncing = false;
        }
    }

    // A toggle's child (add_toggle in adw-inline-view-switcher.c): a widget holding an indicator bin around the title, the
    // icon, or the icon beside the title.
    private Control Toggle(ViewStackPage? page)
    {
        var widget = new AdwBox();
        Css.SetNode(widget, "widget");
        if (page is null)
        {
            return widget;
        }

        AdwLabel Title()
        {
            var label = new AdwLabel { TextAlignment = TextAlignment.Center };
            Css.SetNode(label, "label");
            label.Bind(TextBlock.TextProperty, page.GetObservable(ViewStackPage.TitleProperty));
            label.Bind(AdwLabel.RecognizesAccessKeyProperty, page.GetObservable(ViewStackPage.UseUnderlineProperty));
            label.Bind(TextBlock.TextTrimmingProperty, this.GetObservable(CanShrinkProperty, s => s ? TextTrimming.CharacterEllipsis : TextTrimming.None));
            return label;
        }

        AdwIcon Icon()
        {
            var icon = new AdwIcon();
            icon.Bind(AdwIcon.IconNameProperty, page.GetObservable(ViewStackPage.IconNameProperty));
            return icon;
        }

        IndicatorBin Bin(Control child)
        {
            var bin = new IndicatorBin { Child = child, VerticalAlignment = VerticalAlignment.Center };
            bin.Bind(IndicatorBin.BadgeNumberProperty, page.GetObservable(ViewStackPage.BadgeNumberProperty));
            bin.Bind(IndicatorBin.NeedsAttentionProperty, page.GetObservable(ViewStackPage.NeedsAttentionProperty));
            return bin;
        }

        switch (DisplayMode)
        {
            case InlineViewSwitcherDisplayMode.Icons:
                var icons = Bin(Icon());
                icons.HorizontalAlignment = HorizontalAlignment.Center;
                widget.Child = icons;
                break;
            case InlineViewSwitcherDisplayMode.Both:
                var box = new AdwBox { HorizontalAlignment = HorizontalAlignment.Center };
                Css.SetNode(box, "box");
                Css.SetClasses(box, "horizontal");
                box.Child = new AdwStack { Orientation = Orientation.Horizontal, Children = { Bin(Icon()), Title() } };
                widget.Child = box;
                break;
            default:
                var labels = Bin(Title());
                labels.HorizontalAlignment = HorizontalAlignment.Center;
                widget.Child = labels;
                break;
        }

        return widget;
    }

    /// <summary>A page as a toggle group's item.</summary>
    private sealed record PageItem(ViewStackPage Page);
}
