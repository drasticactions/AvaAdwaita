using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>
/// Keeps one element's <see cref="CssNode"/> in step with the element: its name, classes, states,
/// parent and position, and pushes the node's computed style back to <see cref="Css.StyleProperty"/>.
/// </summary>
internal sealed class NodeBinder
{
    private static readonly ConditionalWeakTable<StyledElement, object> s_subscribed = new();
    private readonly StyledElement _owner;
    private StyledElement? _stateSource;
    private ItemsControl? _itemsOwner;
    private TextProperties? _text;
    private Presenters.TransitionDriver? _transitions;
    private ComputedStyle? _shown;
    private bool _attached;

    private NodeBinder(StyledElement owner)
    {
        _owner = owner;
        Node = new CssNode { Owner = this };
        Node.StyleChanged += OnStyleChanged;
    }

    public CssNode Node { get; }

    public StyledElement Owner => _owner;

    /// <summary>Called when <see cref="Css.NodeProperty"/> changes.</summary>
    public static void Update(StyledElement element)
    {
        if (element is not Visual visual)
        {
            return;
        }

        if (!s_subscribed.TryGetValue(element, out _))
        {
            s_subscribed.Add(element, element);
            visual.AttachedToVisualTree += OnAttached;
            visual.DetachedFromVisualTree += OnDetached;
        }

        var name = element.GetValue(Css.NodeProperty);
        var binder = element.GetValue(Css.BinderProperty);
        if (name is null)
        {
            if (binder is not null)
            {
                Destroy(element, binder);
            }

            return;
        }

        if (binder is not null)
        {
            binder.Refresh();
            return;
        }

        if (visual.IsAttachedToVisualTree())
        {
            Create(element);
        }
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is StyledElement element && element.GetValue(Css.NodeProperty) is not null && element.GetValue(Css.BinderProperty) is null)
        {
            Create(element);
        }
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is StyledElement element && element.GetValue(Css.BinderProperty) is { } binder)
        {
            Destroy(element, binder);
        }
    }

    private static void Create(StyledElement element)
    {
        AdwaitaEnvironment.EnsureInitialized();
        var binder = new NodeBinder(element);
        element.SetValue(Css.BinderProperty, binder);
        binder.Attach();
    }

    private static void Destroy(StyledElement element, NodeBinder binder)
    {
        binder.Detach();
        element.ClearValue(Css.BinderProperty);
        element.ClearValue(Css.StyleProperty);
    }

    private void Attach()
    {
        _attached = true;
        _text = new TextProperties(_owner);
        if (_owner is Visual visual)
        {
            _transitions = new Presenters.TransitionDriver(visual, Show, Node);
        }

        Rebind();
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        Compute();
        NodeTree.Insert(this);

        // Resolve now so the first layout already has the style.
        if (Node.Engine is { } engine)
        {
            engine.Resolve(Node);
        }
    }

    private void Detach()
    {
        _attached = false;
        _text?.Dispose();
        _text = null;
        _transitions = null;
        _shown = null;
        _owner.PropertyChanged -= OnOwnerPropertyChanged;
        UnbindSources();
        NodeTree.Remove(this);
    }

    private void UnbindSources()
    {
        _owner.Classes.CollectionChanged -= OnClassesChanged;
        if (_stateSource is not null && !ReferenceEquals(_stateSource, _owner))
        {
            _stateSource.Classes.CollectionChanged -= OnClassesChanged;
            _stateSource.PropertyChanged -= OnStateSourcePropertyChanged;
        }

        if (_itemsOwner is not null)
        {
            _itemsOwner.ItemsView.CollectionChanged -= OnItemsChanged;
            _itemsOwner = null;
        }

        _stateSource = null;
    }

    public void Rebind()
    {
        UnbindSources();
        _owner.Classes.CollectionChanged += OnClassesChanged;
        _stateSource = StateMapper.StateSourceOf(_owner);
        if (_stateSource is not null && !ReferenceEquals(_stateSource, _owner))
        {
            _stateSource.Classes.CollectionChanged += OnClassesChanged;
            _stateSource.PropertyChanged += OnStateSourcePropertyChanged;
        }

        if (_owner.GetValue(Css.PositionProperty) == CssPositionSource.Item && _owner is Control oc && ItemsControl.ItemsControlFromItemContainer(oc) is { } items)
        {
            _itemsOwner = items;
            items.ItemsView.CollectionChanged += OnItemsChanged;
        }

        if (_attached)
        {
            Refresh();
        }
    }

    /// <summary>Recomputes the node's name, classes and states.</summary>
    public void Refresh()
    {
        if (_attached)
        {
            Compute();
        }
    }

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateItemPosition();

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty)
        {
            Node.Visible = _owner is not Visual v || v.IsVisible;

            // The other items' places among the visible ones change too.
            if ((_itemsOwner ?? (_owner is Control oc ? ItemsControl.ItemsControlFromItemContainer(oc) : null)) is { } items)
            {
                for (var i = 0; i < items.ItemCount; i++)
                {
                    Container(items, i)?.GetValue(Css.BinderProperty)?.UpdateItemPosition();
                }
            }
        }
        else if (e.Property == Visual.FlowDirectionProperty)
        {
            Refresh();
            if (_shown is { } shown)
            {
                Show(shown);
            }
        }
        else if (e.Property == InputElement.IsEffectivelyEnabledProperty || e.Property == Css.SensitiveProperty || CssNodeProviders.IsWatched(_owner, e.Property))
        {
            Refresh();
        }
    }

    private void OnStateSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEffectivelyEnabledProperty || (_stateSource is { } s && CssNodeProviders.IsWatched(s, e.Property)))
        {
            Refresh();
        }
    }

    private void Compute()
    {
        Node.Name = _owner.GetValue(Css.NodeProperty);
        Node.Id = _owner.GetValue(Css.IdProperty);
        var classes = new List<string>();
        foreach (var c in _owner.Classes)
        {
            // Pseudo-classes are states; "adw-" classes are the theme's own markers, not CSS classes.
            if (c.Length > 0 && c[0] != ':' && !c.StartsWith("adw-", StringComparison.Ordinal))
            {
                classes.Add(c);
            }
        }

        if (_owner.GetValue(Css.ClassesProperty) is { } extra)
        {
            foreach (var c in extra.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                classes.Add(c);
            }
        }

        CssNodeProviders.AddClasses(_owner, classes);
        Node.SetClasses(classes);
        Node.States = StateMapper.Map(_owner, _stateSource) | NodeTree.WindowStates(this) | _owner.GetValue(Css.StatesProperty);
        Node.ForcedDark = NodeTree.ScopeDark(_owner);
        Node.Visible = _owner is not Visual v || v.IsVisible;
        UpdateItemPosition();
    }

    // An item's container: one the items control made, or the item, when it is a container itself.
    private static Control? Container(ItemsControl items, int index) =>
        items.ContainerFromIndex(index) ?? (index < items.Items.Count ? items.Items[index] as Control : null);

    private void UpdateItemPosition()
    {
        // A container can attach before its items control links it, so look again until it is found.
        if (_itemsOwner is null && _attached && _owner.GetValue(Css.PositionProperty) == CssPositionSource.Item
            && _owner is Control oc && ItemsControl.ItemsControlFromItemContainer(oc) is { } items)
        {
            _itemsOwner = items;
            items.ItemsView.CollectionChanged += OnItemsChanged;
        }

        if (_itemsOwner is not null && _owner is Control c)
        {
            // Its place among the visible items, as GTK's positional selectors count only visible siblings; an item
            // without a container counts as visible.
            var index = _itemsOwner.IndexFromContainer(c);
            if (index < 0)
            {
                Node.ItemPosition = null;
                return;
            }

            var (visibleIndex, count) = (0, 0);
            for (var i = 0; i < _itemsOwner.ItemCount; i++)
            {
                if (Container(_itemsOwner, i) is { IsVisible: false })
                {
                    continue;
                }

                if (i < index)
                {
                    visibleIndex++;
                }

                count++;
            }

            Node.ItemPosition = (visibleIndex, Math.Max(count, visibleIndex + 1));
        }
        else
        {
            Node.ItemPosition = CssNodeProviders.Position(_owner);
        }
    }

    private void OnStyleChanged(object? sender, CssStyleChangedEventArgs e)
    {
        if (!_attached)
        {
            return;
        }

        if (_transitions is { } t)
        {
            t.Apply(e.OldStyle, e.NewStyle);
        }
        else
        {
            Show(e.NewStyle);
        }
    }

    private void Show(ComputedStyle style)
    {
        if (!_attached)
        {
            return;
        }

        // Avalonia lays out and draws a right-to-left subtree mirrored, so its left and right are swapped back.
        _shown = style;
        var drawn = _owner is Visual { FlowDirection: Avalonia.Media.FlowDirection.RightToLeft } ? style.Mirrored() : style;
        _owner.SetValue(Css.StyleProperty, drawn);
        _text?.Push(drawn);
    }

    /// <summary>The style the owner shows: its node's.</summary>
    public ComputedStyle? Style => Node.Style;
}
