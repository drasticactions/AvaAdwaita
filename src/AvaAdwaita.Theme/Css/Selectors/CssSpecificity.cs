using System;

namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>Selector specificity as GTK counts it: ids, then classes and pseudo-classes, then node names.</summary>
internal readonly record struct CssSpecificity(int Ids, int Classes, int Elements) : IComparable<CssSpecificity>
{
    public int CompareTo(CssSpecificity other)
    {
        if (Ids != other.Ids)
        {
            return Ids.CompareTo(other.Ids);
        }

        if (Classes != other.Classes)
        {
            return Classes.CompareTo(other.Classes);
        }

        return Elements.CompareTo(other.Elements);
    }

    /// <summary>Packs the specificity into one sortable number.</summary>
    public int Packed => (Math.Min(Ids, 1023) << 20) | (Math.Min(Classes, 1023) << 10) | Math.Min(Elements, 1023);

    public static bool operator <(CssSpecificity a, CssSpecificity b) => a.CompareTo(b) < 0;

    public static bool operator >(CssSpecificity a, CssSpecificity b) => a.CompareTo(b) > 0;

    public static bool operator <=(CssSpecificity a, CssSpecificity b) => a.CompareTo(b) <= 0;

    public static bool operator >=(CssSpecificity a, CssSpecificity b) => a.CompareTo(b) >= 0;
}
