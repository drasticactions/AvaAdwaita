using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Computed;

/// <content>Box-model helpers.</content>
public sealed partial class ComputedStyle
{
    internal CssBoxSides Padding => new(Size.PaddingLeft, Size.PaddingTop, Size.PaddingRight, Size.PaddingBottom);

    internal CssBoxSides Margin => new(Size.MarginLeft, Size.MarginTop, Size.MarginRight, Size.MarginBottom);

    internal CssBoxSides BorderWidths => new(Border.LeftWidth, Border.TopWidth, Border.RightWidth, Border.BottomWidth);

    internal CssCorners Radii => new(Border.TopLeftRadius, Border.TopRightRadius, Border.BottomRightRadius, Border.BottomLeftRadius);
}
