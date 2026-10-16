using System;

namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>A 256-bit bloom filter over the names, classes and ids of a node's ancestors.</summary>
/// <remarks>The same idea as GTK's selector tree ancestor filter: a selector that needs an ancestor key the filter lacks cannot match.</remarks>
internal struct CssBloom
{
    private ulong _a;
    private ulong _b;
    private ulong _c;
    private ulong _d;

    public static int NameKey(string name) => Mix(name.GetHashCode(StringComparison.Ordinal));

    public static int ClassKey(string name) => Mix(name.GetHashCode(StringComparison.Ordinal) * 31 + 7);

    public static int IdKey(string name) => Mix(name.GetHashCode(StringComparison.Ordinal) * 17 + 3);

    private static int Mix(int h)
    {
        var x = (uint)h;
        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        x *= 0x846ca68b;
        x ^= x >> 16;
        return (int)x;
    }

    public void Add(int key)
    {
        Set(key & 255);
        Set((key >> 8) & 255);
    }

    public readonly bool MayContain(int key) => Get(key & 255) && Get((key >> 8) & 255);

    /// <summary>True when every bit of <paramref name="mask"/> is set: the filter may hold all the keys the mask was built from.</summary>
    public readonly bool MayContainAll(in CssBloom mask) =>
        (_a & mask._a) == mask._a && (_b & mask._b) == mask._b && (_c & mask._c) == mask._c && (_d & mask._d) == mask._d;

    /// <summary>A mask of the given keys, for <see cref="MayContainAll"/>.</summary>
    public static CssBloom Of(int[] keys)
    {
        var b = default(CssBloom);
        foreach (var k in keys)
        {
            b.Add(k);
        }

        return b;
    }

    public void Union(in CssBloom other)
    {
        _a |= other._a;
        _b |= other._b;
        _c |= other._c;
        _d |= other._d;
    }

    private void Set(int bit)
    {
        var m = 1UL << (bit & 63);
        switch (bit >> 6)
        {
            case 0: _a |= m; break;
            case 1: _b |= m; break;
            case 2: _c |= m; break;
            default: _d |= m; break;
        }
    }

    private readonly bool Get(int bit)
    {
        var m = 1UL << (bit & 63);
        return (bit >> 6) switch
        {
            0 => (_a & m) != 0,
            1 => (_b & m) != 0,
            2 => (_c & m) != 0,
            _ => (_d & m) != 0,
        };
    }
}
