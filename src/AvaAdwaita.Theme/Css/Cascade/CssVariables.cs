using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary><c>var()</c> substitution, as gtkcssvariablevalue.c and css-variables-1.</summary>
internal static class CssVariables
{
    /// <summary>Substitutes every <c>var()</c> in <paramref name="raw"/>, or returns null when one is invalid.</summary>
    /// <param name="raw">The value.</param>
    /// <param name="lookup">Returns a custom property's substituted value, or null when it is not set or invalid.</param>
    public static CssComponent[]? Substitute(IReadOnlyList<CssComponent> raw, Func<string, CssComponent[]?> lookup)
    {
        var output = new List<CssComponent>(raw.Count);
        return Append(raw, lookup, output) ? output.ToArray() : null;
    }

    private static bool Append(IReadOnlyList<CssComponent> raw, Func<string, CssComponent[]?> lookup, List<CssComponent> output)
    {
        foreach (var c in raw)
        {
            if (c.IsFunctionNamed("var"))
            {
                if (!AppendVar(c, lookup, output))
                {
                    return false;
                }

                continue;
            }

            if ((c.IsFunction || c.IsBlock) && c.ContainsVar())
            {
                var inner = new List<CssComponent>(c.Children.Length);
                if (!Append(c.Children, lookup, inner))
                {
                    return false;
                }

                output.Add(new CssComponent(c.Token, inner.ToArray()));
                continue;
            }

            output.Add(c);
        }

        return true;
    }

    private static bool AppendVar(CssComponent fn, Func<string, CssComponent[]?> lookup, List<CssComponent> output)
    {
        var args = fn.Children;
        var i = 0;
        while (i < args.Length && args[i].IsWhitespace)
        {
            i++;
        }

        if (i >= args.Length || args[i].Type != CssTokenType.Ident || !args[i].Token.Text.StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        var name = args[i].Token.Text;
        i++;
        while (i < args.Length && args[i].IsWhitespace)
        {
            i++;
        }

        CssComponent[]? fallback = null;
        if (i < args.Length)
        {
            if (args[i].Type != CssTokenType.Comma)
            {
                return false;
            }

            fallback = args[(i + 1)..];
        }

        var value = lookup(name);
        if (value is not null)
        {
            output.AddRange(value);
            return true;
        }

        if (fallback is not null)
        {
            return Append(Trim(fallback), lookup, output);
        }

        return false;
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

        return a[s..e];
    }

    /// <summary>
    /// Computes a node's custom properties: the parent's, overridden by the node's declarations,
    /// each with its own <c>var()</c> references substituted. A cycle makes its members invalid.
    /// </summary>
    public static CssCustomProperties Compute(CssCustomProperties parent, Dictionary<string, CssDeclaration> declared)
    {
        if (declared.Count == 0)
        {
            return parent;
        }

        var result = new Dictionary<string, CssComponent[]?>(StringComparer.Ordinal);
        var resolving = new HashSet<string>(StringComparer.Ordinal);

        CssComponent[]? Resolve(string name)
        {
            if (result.TryGetValue(name, out var done))
            {
                return done;
            }

            if (!declared.TryGetValue(name, out var decl))
            {
                return parent.TryGet(name, out var inherited) ? inherited : null;
            }

            if (!resolving.Add(name))
            {
                // A cycle: invalid at computed-value time.
                return null;
            }

            var raw = decl.Raw!;
            CssComponent[]? value;
            if (CssStyleSheet.WideKeyword(raw) is { } wide)
            {
                // Custom properties inherit, so inherit and unset keep the parent's; initial is the guaranteed-invalid value.
                value = wide == CssWideKeyword.Initial ? null : parent.TryGet(name, out var p) ? p : null;
            }
            else
            {
                value = CssComponent.ContainsVar(raw) ? Substitute(raw, Resolve) : raw;
            }

            resolving.Remove(name);
            result[name] = value;
            return value;
        }

        foreach (var name in declared.Keys)
        {
            Resolve(name);
        }

        var overrides = new Dictionary<string, CssComponent[]>(StringComparer.Ordinal);
        var removed = false;
        foreach (var (k, v) in result)
        {
            if (v is not null)
            {
                overrides[k] = v;
            }
            else if (parent.TryGet(k, out _))
            {
                removed = true;
            }
        }

        if (!removed)
        {
            return parent.With(overrides);
        }

        var all = new Dictionary<string, CssComponent[]>(StringComparer.Ordinal);
        foreach (var (k, v) in parent.Entries)
        {
            if (!result.ContainsKey(k) || result[k] is not null)
            {
                all[k] = v;
            }
        }

        foreach (var (k, v) in overrides)
        {
            all[k] = v;
        }

        return new CssCustomProperties(all);
    }
}
