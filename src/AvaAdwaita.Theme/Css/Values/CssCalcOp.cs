namespace AvaAdwaita.CssEngine.Values;

/// <summary>The node kinds of a <c>calc()</c> tree.</summary>
internal enum CssCalcOp : byte
{
    Leaf,
    Channel,
    Sum,
    Negate,
    Product,
    Invert,
    Min,
    Max,
    Clamp,
}
