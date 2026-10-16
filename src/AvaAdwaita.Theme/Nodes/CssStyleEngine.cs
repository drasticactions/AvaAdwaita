using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Properties;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>
/// Resolves the styles of CSS node trees against a style set. Dirty nodes are resolved top-down in one
/// pass; a node whose new style is the same object as its old one leaves clean children alone.
/// </summary>
public sealed class CssStyleEngine
{
    private readonly List<CssNode> _roots = new();
    private readonly Dictionary<CssMediaState, CssCascadeResolver> _resolvers = new();
    private CssMediaState _media = CssMediaState.Default;
    private CssEnvironment _env = CssEnvironment.Default;
    private bool _scheduled;
    private int _styleVersion = -1;

    /// <summary>Creates an engine over a style set.</summary>
    public CssStyleEngine(CssStyleSet styles)
    {
        Styles = styles;
        Styles.Changed += (_, _) => InvalidateAll();
    }

    /// <summary>The style sheets.</summary>
    public CssStyleSet Styles { get; }

    /// <summary>
    /// Called when nodes become dirty and a resolve pass is needed. The host posts <see cref="ResolveNow"/>
    /// to its UI loop. Without a callback, styles resolve when read with <see cref="Resolve"/>.
    /// </summary>
    public Action? ResolveRequested { get; set; }

    /// <summary>Raised after a resolve pass.</summary>
    public event EventHandler? Resolved;

    /// <summary>The media state <c>@media</c> queries test.</summary>
    public CssMediaState Media
    {
        get => _media;
        set
        {
            if (_media != value)
            {
                _media = value;
                InvalidateAll();
            }
        }
    }

    /// <summary>The interface font family that the initial <c>font-family</c> means.</summary>
    public string FontFamily
    {
        get => _env.FontFamily;
        set => Environment = _env with { FontFamily = value };
    }

    /// <summary>The interface font size in px that <c>medium</c> and <c>rem</c> mean.</summary>
    public double FontSizePx
    {
        get => _env.FontSizePx;
        set => Environment = _env with { FontSizePx = value };
    }

    internal CssEnvironment Environment
    {
        get => _env;
        set
        {
            if (_env != value)
            {
                _env = value;
                InvalidateAll();
            }
        }
    }

    internal CssStyleInterner Interner { get; } = new();

    /// <summary>The number of styles computed rather than shared, for tests and diagnostics.</summary>
    public int ComputeCount
    {
        get
        {
            var n = 0;
            foreach (var r in _resolvers.Values)
            {
                n += r.ComputeCount;
            }

            return n;
        }
    }

    /// <summary>The root nodes.</summary>
    public IReadOnlyList<CssNode> Roots => _roots;

    /// <summary>Adds a root node; its tree is resolved on the next pass.</summary>
    public void AddRoot(CssNode root)
    {
        if (root.Parent is not null)
        {
            throw new ArgumentException("only a node without a parent can be a root", nameof(root));
        }

        if (!_roots.Contains(root))
        {
            _roots.Add(root);
            root.SetEngine(this);
            root.MarkSubtree();
        }
    }

    /// <summary>Removes a root node.</summary>
    public void RemoveRoot(CssNode root)
    {
        if (_roots.Remove(root))
        {
            root.SetEngine(null);
        }
    }

    /// <summary>Marks every node dirty, as GTK's <c>gtk_style_provider_changed</c>.</summary>
    public void InvalidateAll()
    {
        _resolvers.Clear();
        Interner.Clear();
        foreach (var r in _roots)
        {
            r.MarkSubtree();
        }

        ScheduleResolve();
    }

    internal void ScheduleResolve()
    {
        if (_scheduled)
        {
            return;
        }

        _scheduled = true;
        ResolveRequested?.Invoke();
    }

    internal CssCascadeResolver ResolverFor(CssMediaState media)
    {
        if (_styleVersion != Styles.Version)
        {
            _styleVersion = Styles.Version;
            _resolvers.Clear();
        }

        if (!_resolvers.TryGetValue(media, out var r))
        {
            r = new CssCascadeResolver(Styles.Compile(media), _env, Interner);
            _resolvers[media] = r;
        }

        return r;
    }

    internal bool SiblingsDependOn(CssNode node, CssNodeChange change, CssStates states) =>
        ResolverFor(_media).Styles.Index.SiblingsDependOn(node, change, states);

    /// <summary>Resolves every dirty node now.</summary>
    public void ResolveNow()
    {
        _scheduled = false;
        foreach (var root in _roots.ToArray())
        {
            if (root.SubtreeDirty || root.NeedsResolve)
            {
                Visit(root, null, root.MediaOverride ?? _media, force: false, parentChanged: false);
            }
        }

        Resolved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The rules that match <paramref name="node"/>, in cascade order, as <c>sheet:line selector</c>.</summary>
    public IReadOnlyList<string> DescribeMatches(CssNode node)
    {
        var result = new List<string>();
        foreach (var e in ResolverFor(node.MediaOverride ?? _media).MatchingRules(node))
        {
            result.Add($"{e.Sheet.Name}:{e.Rule.Line} {e.Selector}");
        }

        return result;
    }

    /// <summary>Returns the node's style, resolving the dirty part of its tree first.</summary>
    public ComputedStyle Resolve(CssNode node)
    {
        if (node.Style is null || node.NeedsResolve || node.SubtreeDirty || AncestorDirty(node))
        {
            ResolveNow();
        }

        return node.Style ?? throw new InvalidOperationException("the node is not in a tree of this engine");
    }

    private static bool AncestorDirty(CssNode node)
    {
        for (var n = node.Parent; n is not null; n = n.Parent)
        {
            if (n.NeedsResolve)
            {
                return true;
            }
        }

        return false;
    }

    private void Visit(CssNode node, ComputedStyle? parentStyle, CssMediaState media, bool force, bool parentChanged)
    {
        var resolve = force || parentChanged || node.NeedsResolve || node.Style is null;
        var changed = false;
        var childForce = force;
        if (node.MediaOverride is { } m)
        {
            media = m;
        }

        if (node.ForcedDark is { } dark && media.Dark != dark)
        {
            media = media with { Dark = dark };
        }

        if (resolve)
        {
            var old = node.Style;
            var style = ResolverFor(media).Resolve(node, parentStyle);
            childForce = force || node.ForceSubtree;
            node.NeedsResolve = false;
            node.ForceSubtree = false;
            changed = !ReferenceEquals(old, style);
            node.SetStyle(style);
        }
        else
        {
            CssCascadeResolver.UpdateBloom(node);
        }

        if (resolve || node.SubtreeDirty)
        {
            node.SubtreeDirty = false;
            var style = node.Style!;
            foreach (var c in node.Children)
            {
                if (childForce || changed || c.NeedsResolve || c.SubtreeDirty || c.Style is null)
                {
                    Visit(c, style, media, childForce, changed);
                }
            }
        }
    }
}
