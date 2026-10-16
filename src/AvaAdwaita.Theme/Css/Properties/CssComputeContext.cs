using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>The state of one node's computation: the parent's style and the values computed so far.</summary>
internal sealed class CssComputeContext
{
    public CssComputeContext(ICssColorScope colors, CssEnvironment env)
    {
        Colors = colors;
        Env = env;
    }

    public ICssColorScope Colors { get; set; }

    public CssEnvironment Env { get; set; }

    public ComputedStyle? Parent { get; set; }

    /// <summary>Computed values by property id. <c>color</c>, <c>-gtk-dpi</c> and <c>font-size</c> are computed first.</summary>
    public object?[] Values { get; } = new object?[CssPropertyTable.Count];

    /// <summary>The property being computed.</summary>
    public int PropertyId { get; set; }

    public CssColor Color => Values[CssPropertyTable.Id.Color] is CssColor c ? c : ParentColor;

    public CssColor ParentColor => Parent?.Core.Color ?? CssColor.White;

    public double Dpi => Values[CssPropertyTable.Id.Dpi] is double d ? d : Parent?.Core.Dpi ?? 96;

    public double FontSize => Values[CssPropertyTable.Id.FontSize] is double f ? f : ParentFontSize;

    public double ParentFontSize => Parent?.Core.FontSize ?? Env.FontSizePx;

    /// <summary>The numeric context for lengths: em is this node's font size.</summary>
    public CssNumericContext Numeric => new(FontSize, Env.FontSizePx, Dpi);

    /// <summary>The numeric context for <c>font-size</c> itself: em is the parent's font size.</summary>
    public CssNumericContext NumericForFontSize => new(ParentFontSize, Env.FontSizePx, Dpi);

    public void Reset(ComputedStyle? parent)
    {
        Parent = parent;
        System.Array.Clear(Values);
    }
}
