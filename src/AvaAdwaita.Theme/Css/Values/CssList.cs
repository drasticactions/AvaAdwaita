using System;
using System.Collections;
using System.Collections.Generic;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>An immutable list with structural equality, for computed values.</summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class CssList<T> : IReadOnlyList<T>, IEquatable<CssList<T>>
{
    private readonly T[] _items;
    private readonly int _hash;

    /// <summary>Creates a list over <paramref name="items"/>, which must not change afterwards.</summary>
    public CssList(T[] items)
    {
        _items = items;
        var h = new HashCode();
        foreach (var i in items)
        {
            h.Add(i);
        }

        _hash = h.ToHashCode();
    }

    /// <summary>The empty list.</summary>
    public static CssList<T> Empty { get; } = new([]);

    /// <inheritdoc/>
    public int Count => _items.Length;

    /// <inheritdoc/>
    public T this[int index] => _items[index];

    /// <summary>The item at <paramref name="index"/> modulo the count, as CSS repeats layered lists.</summary>
    public T Cycle(int index) => _items[index % _items.Length];

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(CssList<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_hash != other._hash || _items.Length != other._items.Length)
        {
            return false;
        }

        var cmp = EqualityComparer<T>.Default;
        for (var i = 0; i < _items.Length; i++)
        {
            if (!cmp.Equals(_items[i], other._items[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CssList<T> l && Equals(l);

    /// <inheritdoc/>
    public override int GetHashCode() => _hash;

    /// <inheritdoc/>
    public override string ToString() => _items.Length == 0 ? "none" : string.Join(", ", _items);
}
