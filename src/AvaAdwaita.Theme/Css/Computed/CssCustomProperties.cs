using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Computed;

/// <summary>The computed custom properties of a node: names to substituted component lists. Immutable and shared down the tree.</summary>
public sealed class CssCustomProperties : IEquatable<CssCustomProperties>
{
    private readonly Dictionary<string, CssComponent[]> _values;
    private int? _hash;

    internal CssCustomProperties(Dictionary<string, CssComponent[]> values)
    {
        _values = values;
    }

    /// <summary>No custom properties.</summary>
    public static CssCustomProperties Empty { get; } = new(new Dictionary<string, CssComponent[]>(StringComparer.Ordinal));

    /// <summary>The number of custom properties.</summary>
    public int Count => _values.Count;

    internal bool TryGet(string name, out CssComponent[] value) => _values.TryGetValue(name, out value!);

    internal IEnumerable<KeyValuePair<string, CssComponent[]>> Entries => _values;

    /// <summary>The value of a custom property as CSS text, or null.</summary>
    public string? Get(string name) => _values.TryGetValue(name, out var v) ? CssComponent.ToCss(v) : null;

    /// <summary>The names, in no particular order.</summary>
    public IEnumerable<string> Names => _values.Keys;

    internal CssCustomProperties With(Dictionary<string, CssComponent[]> overrides)
    {
        var d = new Dictionary<string, CssComponent[]>(_values, StringComparer.Ordinal);
        foreach (var (k, v) in overrides)
        {
            d[k] = v;
        }

        return new CssCustomProperties(d);
    }

    /// <inheritdoc/>
    public bool Equals(CssCustomProperties? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_values.Count != other._values.Count || GetHashCode() != other.GetHashCode())
        {
            return false;
        }

        foreach (var (k, v) in _values)
        {
            if (!other._values.TryGetValue(k, out var ov) || !CssComponent.ListEquals(v, ov))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CssCustomProperties c && Equals(c);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (_hash is { } h)
        {
            return h;
        }

        var acc = 0;
        foreach (var (k, v) in _values)
        {
            acc ^= HashCode.Combine(k, CssComponent.ListHash(v));
        }

        _hash = acc;
        return acc;
    }
}
