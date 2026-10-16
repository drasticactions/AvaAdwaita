using System;
using System.Collections.Generic;
using System.Text;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>
/// One CSS node, the mirror of a <c>GtkCssNode</c>: a name, an id, classes, state flags, a parent and
/// siblings, and the computed style. Changing what selectors read marks the node for re-resolution.
/// </summary>
public sealed class CssNode
{
    private static readonly string[] s_noClasses = [];
    private readonly List<CssNode> _children = new();
    private string? _name;
    private string? _id;
    private string[] _classes = s_noClasses;
    private CssStates _states;
    private bool _visible = true;
    private (int Index, int Count)? _itemPosition;
    private CssStyleEngine? _engine;

    /// <summary>Creates a node with a name.</summary>
    public CssNode(string? name = null)
    {
        _name = name;
    }

    /// <summary>Raised after the computed style changes.</summary>
    public event EventHandler<CssStyleChangedEventArgs>? StyleChanged;

    /// <summary>The node name, as <c>button</c> or <c>label</c>.</summary>
    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                Invalidate(CssNodeChange.Name);
            }
        }
    }

    /// <summary>The <c>#id</c>.</summary>
    public string? Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                Invalidate(CssNodeChange.Name);
            }
        }
    }

    /// <summary>The style classes.</summary>
    public IReadOnlyList<string> Classes => _classes;

    /// <summary>The state flags.</summary>
    public CssStates States
    {
        get => _states;
        set
        {
            if (_states != value)
            {
                var changed = _states ^ value;
                _states = value;
                Invalidate(CssNodeChange.State, changed);
            }
        }
    }

    /// <summary>
    /// False hides the node from positional selectors, as GTK skips invisible widgets for
    /// <c>:first-child</c> and <c>+</c>.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                Parent?.InvalidateChildPositions();
            }
        }
    }

    /// <summary>
    /// The position among items, when it comes from an items control rather than from siblings,
    /// as virtualized lists do. Null uses the sibling order.
    /// </summary>
    public (int Index, int Count)? ItemPosition
    {
        get => _itemPosition;
        set
        {
            if (_itemPosition != value)
            {
                _itemPosition = value;
                Invalidate(CssNodeChange.Position);
            }
        }
    }

    /// <summary>The parent node.</summary>
    public CssNode? Parent { get; private set; }

    /// <summary>The child nodes, in order.</summary>
    public IReadOnlyList<CssNode> Children => _children;

    /// <summary>The computed style, or null before the first resolution.</summary>
    public ComputedStyle? Style { get; private set; }

    /// <summary>The element this node belongs to.</summary>
    public object? Owner { get; set; }

    /// <summary>The engine of the tree this node is in.</summary>
    public CssStyleEngine? Engine => _engine;

    /// <summary>
    /// For a root node, overrides the media state of its subtree, as a fixed theme variant does,
    /// or null to use the engine's state.
    /// </summary>
    public CssEngine.Cascade.CssMediaState? MediaOverride { get; set; }

    /// <summary>
    /// Forces the dark state of this node's subtree and keeps the rest of the engine's media state, as a
    /// ThemeVariantScope with a fixed variant does; null follows the parent.
    /// </summary>
    public bool? ForcedDark
    {
        get => _forcedDark;
        set
        {
            if (_forcedDark != value)
            {
                _forcedDark = value;
                Invalidate(CssNodeChange.Name);
            }
        }
    }

    private bool? _forcedDark;

    internal bool NeedsResolve { get; set; } = true;

    internal bool ForceSubtree { get; set; } = true;

    internal bool SubtreeDirty { get; set; } = true;

    internal CssBloom AncestorBloom;

    internal bool IsRoot => Parent is null;

    /// <summary>Adds a class. Returns false when it was already there.</summary>
    public bool AddClass(string name)
    {
        if (Array.IndexOf(_classes, name) >= 0)
        {
            return false;
        }

        var a = new string[_classes.Length + 1];
        _classes.CopyTo(a, 0);
        a[^1] = name;
        _classes = a;
        Invalidate(CssNodeChange.Classes);
        return true;
    }

    /// <summary>Removes a class. Returns false when it was not there.</summary>
    public bool RemoveClass(string name)
    {
        var i = Array.IndexOf(_classes, name);
        if (i < 0)
        {
            return false;
        }

        var a = new string[_classes.Length - 1];
        Array.Copy(_classes, 0, a, 0, i);
        Array.Copy(_classes, i + 1, a, i, _classes.Length - i - 1);
        _classes = a;
        Invalidate(CssNodeChange.Classes);
        return true;
    }

    /// <summary>Replaces all classes.</summary>
    public void SetClasses(IEnumerable<string> classes)
    {
        var list = new List<string>();
        foreach (var c in classes)
        {
            if (!string.IsNullOrEmpty(c) && !list.Contains(c))
            {
                list.Add(c);
            }
        }

        if (list.Count == _classes.Length && list.TrueForAll(c => Array.IndexOf(_classes, c) >= 0))
        {
            return;
        }

        _classes = list.Count == 0 ? s_noClasses : list.ToArray();
        Invalidate(CssNodeChange.Classes);
    }

    /// <summary>True when the node has the class.</summary>
    public bool HasClass(string name) => Array.IndexOf(_classes, name) >= 0;

    /// <summary>Sets or clears state flags.</summary>
    public void SetState(CssStates flags, bool on) => States = on ? _states | flags : _states & ~flags;

    /// <summary>Inserts a child at <paramref name="index"/>, or at the end when it is out of range.</summary>
    public void InsertChild(int index, CssNode child)
    {
        if (child.Parent is not null)
        {
            child.Parent.RemoveChild(child);
        }

        if (index < 0 || index > _children.Count)
        {
            index = _children.Count;
        }

        _children.Insert(index, child);
        child.Parent = this;
        child.SetEngine(_engine);
        child.MarkSubtree();
        InvalidateChildPositions();
    }

    /// <summary>Appends a child.</summary>
    public void AppendChild(CssNode child) => InsertChild(_children.Count, child);

    /// <summary>Removes a child.</summary>
    public void RemoveChild(CssNode child)
    {
        if (!_children.Remove(child))
        {
            return;
        }

        child.Parent = null;
        child.SetEngine(null);
        child.MarkSubtree();
        InvalidateChildPositions();
    }

    /// <summary>Attaches a root node to an engine, or detaches it with null.</summary>
    internal void SetEngine(CssStyleEngine? engine)
    {
        if (_engine == engine)
        {
            return;
        }

        _engine = engine;
        foreach (var c in _children)
        {
            c.SetEngine(engine);
        }
    }

    internal void MarkSubtree()
    {
        NeedsResolve = true;
        ForceSubtree = true;
        PropagateDirty();
    }

    private void InvalidateChildPositions()
    {
        // Positions changed, so every child's positional pseudo-classes and sibling selectors may change.
        foreach (var c in _children)
        {
            c.NeedsResolve = true;
            c.ForceSubtree = true;
        }

        SubtreeDirty = true;
        PropagateDirty();
    }

    private void Invalidate(CssNodeChange change, CssStates changedStates = 0)
    {
        NeedsResolve = true;

        // Descendant selectors read ancestors' names, classes and states, so the subtree re-matches.
        if (_children.Count > 0)
        {
            ForceSubtree = true;
        }

        if (Parent is not null && _engine?.SiblingsDependOn(this, change, changedStates) == true)
        {
            var found = false;
            foreach (var s in Parent._children)
            {
                if (found)
                {
                    s.NeedsResolve = true;
                    s.ForceSubtree = true;
                }

                found |= ReferenceEquals(s, this);
            }
        }

        PropagateDirty();
    }

    private void PropagateDirty()
    {
        for (var n = Parent; n is not null && !n.SubtreeDirty; n = n.Parent)
        {
            n.SubtreeDirty = true;
        }

        SubtreeDirty = true;
        _engine?.ScheduleResolve();
    }

    internal void SetStyle(ComputedStyle style)
    {
        var old = Style;
        Style = style;
        if (!ReferenceEquals(old, style))
        {
            StyleChanged?.Invoke(this, new CssStyleChangedEventArgs(old, style));
        }
    }

    internal void ClearStyle() => Style = null;

    internal CssNode? PreviousVisibleSibling
    {
        get
        {
            if (Parent is null)
            {
                return null;
            }

            var list = Parent._children;
            var i = list.IndexOf(this) - 1;
            for (; i >= 0; i--)
            {
                if (list[i]._visible)
                {
                    return list[i];
                }
            }

            return null;
        }
    }

    /// <summary>The 1-based position among visible siblings, or from <see cref="ItemPosition"/>.</summary>
    internal int PositionFromStart
    {
        get
        {
            if (_itemPosition is { } p)
            {
                return p.Index + 1;
            }

            if (Parent is null)
            {
                return 1;
            }

            var pos = 1;
            foreach (var s in Parent._children)
            {
                if (ReferenceEquals(s, this))
                {
                    return pos;
                }

                if (s._visible)
                {
                    pos++;
                }
            }

            return pos;
        }
    }

    /// <summary>The 1-based position from the end among visible siblings.</summary>
    internal int PositionFromEnd
    {
        get
        {
            if (_itemPosition is { } p)
            {
                return p.Count - p.Index;
            }

            if (Parent is null)
            {
                return 1;
            }

            var pos = 1;
            var list = Parent._children;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var s = list[i];
                if (ReferenceEquals(s, this))
                {
                    return pos;
                }

                if (s._visible)
                {
                    pos++;
                }
            }

            return pos;
        }
    }

    /// <summary>The node as a selector, as GTK Inspector shows it: <c>button.flat:hover</c>.</summary>
    public string Describe()
    {
        var sb = new StringBuilder(_name ?? "*");
        if (_id is not null)
        {
            sb.Append('#').Append(_id);
        }

        foreach (var c in _classes)
        {
            sb.Append('.').Append(c);
        }

        sb.Append(CssStateNames.Format(_states));
        return sb.ToString();
    }

    /// <summary>The path from the root, as <c>window.csd &gt; headerbar &gt; button.flat:hover</c>.</summary>
    public string Path
    {
        get
        {
            var parts = new List<string>();
            for (var n = this; n is not null; n = n.Parent)
            {
                parts.Add(n.Describe());
            }

            parts.Reverse();
            return string.Join(" > ", parts);
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Describe();
}
