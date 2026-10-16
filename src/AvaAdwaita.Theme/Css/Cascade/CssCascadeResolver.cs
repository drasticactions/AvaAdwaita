using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Properties;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>
/// Computes a node's style: matches the rule index, orders the declarations, substitutes
/// <c>var()</c>, and computes every property from the parent's style.
/// </summary>
/// <remarks>Follows gtk_css_static_style_compute_value and gtk_css_lookup_resolve (gtkcssstaticstyle.c).</remarks>
internal sealed class CssCascadeResolver
{
    private readonly List<CssRuleEntry> _matched = new();
    private readonly CssDeclaration?[] _winners = new CssDeclaration?[CssPropertyTable.Count];
    private readonly Dictionary<string, CssDeclaration> _customs = new(StringComparer.Ordinal);
    private readonly CssComputeContext _ctx;
    private readonly Dictionary<StyleKey, (ComputedStyle Style, CssRuleEntry[] Rules)> _styleCache = new();
    private readonly ConditionalWeakTable<CssDeclaration, Dictionary<CssCustomProperties, object?>> _varCache = new();
    private readonly ConditionalWeakTable<CssPendingShorthand, Dictionary<CssCustomProperties, object?[]?>> _shorthandCache = new();

    public CssCascadeResolver(CssCompiledStyles styles, CssEnvironment env, CssStyleInterner interner)
    {
        Styles = styles;
        Env = env;
        Interner = interner;
        _ctx = new CssComputeContext(styles, env);
    }

    public CssCompiledStyles Styles { get; }

    public CssEnvironment Env { get; }

    public CssStyleInterner Interner { get; }

    private static readonly CssProperty s_colorProperty = CssPropertyTable.Lookup("color")!;

    /// <summary>The number of styles computed rather than found in the cache.</summary>
    public int ComputeCount { get; private set; }

    /// <summary>Lists the rules that match a node, in cascade order, for inspection.</summary>
    public List<CssRuleEntry> MatchingRules(CssNode node)
    {
        UpdateBloom(node);
        var list = new List<CssRuleEntry>();
        Styles.Index.Match(node, list);
        return list;
    }

    public static void UpdateBloom(CssNode node)
    {
        var bloom = default(Selectors.CssBloom);
        if (node.Parent is { } p)
        {
            bloom = p.AncestorBloom;
            if (p.Name is { } n)
            {
                bloom.Add(Selectors.CssBloom.NameKey(n));
            }

            if (p.Id is { } id)
            {
                bloom.Add(Selectors.CssBloom.IdKey(id));
            }

            foreach (var c in p.Classes)
            {
                bloom.Add(Selectors.CssBloom.ClassKey(c));
            }
        }

        node.AncestorBloom = bloom;
    }

    /// <summary>Computes the style of <paramref name="node"/> given its parent's style.</summary>
    /// <remarks>The node's ancestor filter must be current: resolve top-down.</remarks>
    public ComputedStyle Resolve(CssNode node, ComputedStyle? parent)
    {
        UpdateBloom(node);
        Styles.Index.Match(node, _matched);

        var key = new StyleKey(parent, Signature(_matched), _matched.Count);
        if (_styleCache.TryGetValue(key, out var cached) && SameRules(cached.Rules, _matched))
        {
            return cached.Style;
        }

        var style = Compute(parent);
        _styleCache[key] = (style, _matched.ToArray());
        return style;
    }

    /// <summary>
    /// Computes <paramref name="node"/>'s style with a keyframe's declarations over its matching rules, as an
    /// animation's value at that keyframe. Not cached.
    /// </summary>
    internal ComputedStyle ResolveKeyframe(CssNode node, ComputedStyle? parent, CssDeclaration[] frame)
    {
        UpdateBloom(node);
        Styles.Index.Match(node, _matched);
        return Compute(parent, frame);
    }

    /// <summary>Computes a style from <see cref="_matched"/>, then <paramref name="extra"/> over them.</summary>
    private ComputedStyle Compute(ComputedStyle? parent, CssDeclaration[]? extra = null)
    {
        ComputedStyle result;
        ComputeCount++;
        Array.Clear(_winners);
        _customs.Clear();
        foreach (var e in _matched)
        {
            foreach (var d in e.Rule.Declarations)
            {
                if (d.IsCustom)
                {
                    _customs[d.CustomName!] = d;
                }
                else
                {
                    _winners[d.PropertyId] = d;
                }
            }
        }

        foreach (var d in extra ?? [])
        {
            if (d.IsCustom)
            {
                _customs[d.CustomName!] = d;
            }
            else
            {
                _winners[d.PropertyId] = d;
            }
        }

        var custom = CssVariables.Compute(parent?.Custom ?? CssCustomProperties.Empty, _customs);
        if (!ReferenceEquals(custom, parent?.Custom))
        {
            custom = Interner.Intern(custom);
        }

        var ctx = _ctx;
        ctx.Reset(parent);
        Dictionary<int, object>? dependents = null;
        object? ownColorSpec = null;
        var colorInherited = false;
        var all = CssPropertyTable.All;
        for (var id = 0; id < all.Length; id++)
        {
            var prop = all[id];
            ctx.PropertyId = id;
            var decl = _winners[id];
            var spec = decl is null ? null : SpecifiedValue(decl, custom);
            if (spec is CssWideKeyword wide)
            {
                spec = wide switch
                {
                    CssWideKeyword.Initial => prop.Initial,
                    CssWideKeyword.Inherit => null,
                    _ => prop.Inherited ? null : prop.Initial,
                };

                if (wide == CssWideKeyword.Inherit && parent is null)
                {
                    spec = prop.Initial;
                }
                else if (wide == CssWideKeyword.Inherit)
                {
                    ctx.Values[id] = Inherit(id, parent!, ref dependents);
                    colorInherited |= ReferenceEquals(prop, s_colorProperty);
                    continue;
                }
            }

            if (spec is null)
            {
                if (prop.Inherited && parent is not null)
                {
                    ctx.Values[id] = Inherit(id, parent, ref dependents);
                    colorInherited |= ReferenceEquals(prop, s_colorProperty);
                    continue;
                }

                spec = prop.Initial;
            }

            ctx.Values[id] = prop.Kind.Compute(spec, ctx);
            if (ReferenceEquals(prop, s_colorProperty) && prop.Kind.UsesCurrentColor(spec))
            {
                ownColorSpec = spec;
            }

            // currentColor in 'color' itself is the parent's color: its computed value is absolute and inherits as is.
            if (prop.Inherited && !ReferenceEquals(prop, s_colorProperty) && prop.Kind.UsesCurrentColor(spec))
            {
                dependents ??= new Dictionary<int, object>();
                dependents[id] = spec;
            }
        }

        // GTK computes an icon image's colors from the style's color value, which a node that inherits its color
        // shares with the ancestor that declared it; resolving that value again against the node's own color mixes
        // a color-mix() with currentColor twice (gtk_css_image_icon_theme_compute and _resolve).
        if (ownColorSpec is not null)
        {
            dependents ??= new Dictionary<int, object>();
            dependents[ComputedStyle.SymbolicColorKey] = ownColorSpec;
        }
        else if (colorInherited && parent?.CurrentColorDependents is { } pd && pd.TryGetValue(ComputedStyle.SymbolicColorKey, out var symbolic))
        {
            dependents ??= new Dictionary<int, object>();
            dependents[ComputedStyle.SymbolicColorKey] = symbolic;
            ctx.PropertyId = CssPropertyTable.Id.IconSource;
            dependents[ComputedStyle.IconSourceColorKey] = s_colorProperty.Kind.Compute(symbolic, ctx);
        }

        // A color filter changes everything the node and its children draw, after the children's own filters.
        if (ColorMatrix((CssList<CssFilterFunction>)ctx.Values[CssPropertyTable.Id.Filter]!, parent?.ColorMatrix) is { } matrix)
        {
            dependents ??= new Dictionary<int, object>();
            dependents[ComputedStyle.ColorMatrixKey] = matrix;
        }

        result = ComputedStyle.Create(ctx.Values, parent, custom, dependents, Interner);
        return result;
    }

    private object? Inherit(int id, ComputedStyle parent, ref Dictionary<int, object>? dependents)
    {
        // An inherited value that refers to currentColor recomputes with this node's color, as GTK keeps currentColor symbolic.
        if (parent.CurrentColorDependents is { } deps && deps.TryGetValue(id, out var spec))
        {
            dependents ??= new Dictionary<int, object>();
            dependents[id] = spec;
            return CssPropertyTable.All[id].Kind.Compute(spec, _ctx);
        }

        return parent.GetValue(id);
    }

    // The color filters in order, as one matrix (opacity() fades the node as a group instead), then the parent's.
    private static CssColorMatrix? ColorMatrix(CssList<CssFilterFunction> filters, CssColorMatrix? parent)
    {
        CssColorMatrix? own = null;
        foreach (var f in filters)
        {
            if (CssColorMatrix.Of(f) is { } m)
            {
                own = own is null ? m : own.Then(m);
            }
        }

        return own is null ? parent : parent is null ? own : own.Then(parent);
    }

    /// <summary>The specified value of a declaration after <c>var()</c> substitution, or null when invalid at computed-value time.</summary>
    private object? SpecifiedValue(CssDeclaration decl, CssCustomProperties custom)
    {
        if (decl.Pending is { } pending)
        {
            var values = pending.HasVar ? ExpandShorthand(pending, custom) : pending.StaticValues;
            return values?[decl.PendingIndex];
        }

        if (!decl.HasVar)
        {
            return decl.Value;
        }

        var cache = _varCache.GetOrCreateValue(decl);
        if (cache.TryGetValue(custom, out var cachedValue))
        {
            return cachedValue;
        }

        object? result = null;
        var substituted = CssVariables.Substitute(decl.Raw!, n => custom.TryGet(n, out var x) ? x : null);
        if (substituted is not null)
        {
            if (CssStyleSheet.WideKeyword(Trim(substituted)) is { } wide)
            {
                result = wide;
            }
            else
            {
                var r = new CssReader(substituted);
                var parsed = CssPropertyTable.All[decl.PropertyId].Kind.Parse(r, decl.Context ?? CssParseContext.Default);
                if (parsed is not null && r.AtEnd)
                {
                    result = parsed;
                }
            }
        }

        cache[custom] = result;
        return result;
    }

    private object?[]? ExpandShorthand(CssPendingShorthand pending, CssCustomProperties custom)
    {
        var cache = _shorthandCache.GetOrCreateValue(pending);
        if (cache.TryGetValue(custom, out var cached))
        {
            return cached;
        }

        object?[]? result = null;
        var substituted = CssVariables.Substitute(pending.Raw, n => custom.TryGet(n, out var x) ? x : null);
        if (substituted is not null)
        {
            if (CssStyleSheet.WideKeyword(Trim(substituted)) is { } wide)
            {
                result = new object?[pending.Shorthand.Longhands.Length];
                Array.Fill(result, wide);
            }
            else
            {
                result = pending.Expand(substituted);
            }
        }

        cache[custom] = result;
        return result;
    }

    private static CssComponent[] Trim(CssComponent[] a)
    {
        var s = 0;
        var e = a.Length;
        while (s < e && a[s].IsWhitespace)
        {
            s++;
        }

        while (e > s && a[e - 1].IsWhitespace)
        {
            e--;
        }

        return s == 0 && e == a.Length ? a : a[s..e];
    }

    private static long Signature(List<CssRuleEntry> matched)
    {
        var h = new HashCode();
        foreach (var e in matched)
        {
            h.Add(RuntimeHelpers.GetHashCode(e));
        }

        return h.ToHashCode();
    }

    // The rules are kept with the cache entry, not with the style: interned styles are shared by nodes that
    // matched different rules and computed the same values.
    private static bool SameRules(CssRuleEntry[] rules, List<CssRuleEntry> matched)
    {
        if (rules.Length != matched.Count)
        {
            return false;
        }

        for (var i = 0; i < rules.Length; i++)
        {
            if (!ReferenceEquals(rules[i], matched[i]))
            {
                return false;
            }
        }

        return true;
    }

    private readonly record struct StyleKey(ComputedStyle? Parent, long Signature, int Count);
}
