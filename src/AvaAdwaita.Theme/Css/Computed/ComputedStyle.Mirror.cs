using System.Runtime.CompilerServices;
using AvaAdwaita.CssEngine.Properties;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Computed;

/// <content>The style mirrored left to right.</content>
public sealed partial class ComputedStyle
{
    private static readonly ConditionalWeakTable<ComputedStyle, ComputedStyle> s_mirrored = new();

    /// <summary>
    /// The style with its left and right swapped, for a node Avalonia lays out and draws mirrored: a right-to-left
    /// subtree is laid out left to right and flipped, while GTK's left and right stay on the screen's left and right.
    /// Icon transforms and shadows are left alone, as icons are drawn unmirrored.
    /// </summary>
    internal ComputedStyle Mirrored() => s_mirrored.GetValue(this, static s => s.Mirror());

    private ComputedStyle Mirror()
    {
        var v = new object?[CssPropertyTable.Count];
        CopyTo(v);
        Swap(v, CssPropertyTable.Id.MarginLeft, CssPropertyTable.Id.MarginRight);
        Swap(v, CssPropertyTable.Id.PaddingLeft, CssPropertyTable.Id.PaddingRight);
        Swap(v, CssPropertyTable.Id.BorderLeftWidth, CssPropertyTable.Id.BorderRightWidth);
        Swap(v, CssPropertyTable.Id.BorderLeftStyle, CssPropertyTable.Id.BorderRightStyle);
        Swap(v, CssPropertyTable.Id.BorderLeftColor, CssPropertyTable.Id.BorderRightColor);
        Swap(v, CssPropertyTable.Id.BorderTopLeftRadius, CssPropertyTable.Id.BorderTopRightRadius);
        Swap(v, CssPropertyTable.Id.BorderBottomLeftRadius, CssPropertyTable.Id.BorderBottomRightRadius);
        v[CssPropertyTable.Id.BorderImageSlice] = Mirror((CssBoxValues)v[CssPropertyTable.Id.BorderImageSlice]!);
        v[CssPropertyTable.Id.BorderImageWidth] = Mirror((CssBoxValues)v[CssPropertyTable.Id.BorderImageWidth]!);
        v[CssPropertyTable.Id.BorderImageSource] = Mirror((CssImage?)v[CssPropertyTable.Id.BorderImageSource]);
        v[CssPropertyTable.Id.BoxShadow] = Map((CssList<CssShadow>)v[CssPropertyTable.Id.BoxShadow]!, s => s with { X = -s.X });
        v[CssPropertyTable.Id.BackgroundPosition] = Map((CssList<CssPosition>)v[CssPropertyTable.Id.BackgroundPosition]!, Mirror);
        v[CssPropertyTable.Id.BackgroundImage] = Map((CssList<CssImage?>)v[CssPropertyTable.Id.BackgroundImage]!, Mirror);
        v[CssPropertyTable.Id.Transform] = Map((CssList<CssTransformFunction>)v[CssPropertyTable.Id.Transform]!, Mirror);
        v[CssPropertyTable.Id.TransformOrigin] = Mirror((CssPosition)v[CssPropertyTable.Id.TransformOrigin]!);
        return Create(v, null, Custom, CurrentColorDependents, new CssStyleInterner());
    }

    private static void Swap(object?[] v, int a, int b) => (v[a], v[b]) = (v[b], v[a]);

    private static CssList<T> Map<T>(CssList<T> list, System.Func<T, T> f)
    {
        if (list.Count == 0)
        {
            return list;
        }

        var items = new T[list.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = f(list[i]);
        }

        return new CssList<T>(items);
    }

    private static CssBoxValues Mirror(CssBoxValues b) => b with { Left = b.Right, Right = b.Left };

    // A position x from the left is 100% - x from the right; a background's percentage is of the room the image
    // leaves, so the same holds for it.
    private static CssNumber MirrorX(CssNumber x) => new(-x.Value, 100 - x.Percent, x.Class == CssDimensionClass.Length ? CssDimensionClass.Length : CssDimensionClass.Percent);

    private static CssPosition Mirror(CssPosition p) => p with { X = MirrorX(p.X) };

    private static CssImage? Mirror(CssImage? image) => image switch
    {
        CssLinearGradient { Side: 0 } g => g with { Angle = -g.Angle },
        CssLinearGradient g => g with { Side = MirrorSide(g.Side) },
        CssRadialGradient g => g with { Center = Mirror(g.Center) },
        _ => image,
    };

    private static int MirrorSide(int side) =>
        (side & ~(CssLinearGradient.Left | CssLinearGradient.Right))
        | ((side & CssLinearGradient.Left) != 0 ? CssLinearGradient.Right : 0)
        | ((side & CssLinearGradient.Right) != 0 ? CssLinearGradient.Left : 0);

    // Mirroring a transform is flipping x before and after it.
    private static CssTransformFunction Mirror(CssTransformFunction f) => f.Kind switch
    {
        CssTransformKind.Translate => f with { Tx = new CssNumber(-f.Tx.Value, -f.Tx.Percent, f.Tx.Class) },
        CssTransformKind.Rotate => f with { A = -f.A },
        CssTransformKind.Skew or CssTransformKind.SkewX or CssTransformKind.SkewY => f with { A = -f.A, B = -f.B },
        CssTransformKind.Matrix => f with { B = -f.B, C = -f.C, E = -f.E },
        _ => f,
    };
}
