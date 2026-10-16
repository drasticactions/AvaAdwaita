using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.Markup;

namespace AvaAdwaita.Nodes;

/// <summary>
/// Binds the inherited text properties of a node's element to its computed style, at style priority,
/// so plain text inside a template (a content presenter's text block) gets the node's color and font.
/// A text block that is a node itself, as a data grid's cell text, also gets the label's line height, letter
/// spacing and box, as an <see cref="Presenters.AdwLabel"/> does.
/// </summary>
internal sealed class TextProperties : IDisposable
{
    private readonly StyleObservable<IBrush?> _foreground = new();
    private readonly StyleObservable<FontFamily> _family = new();
    private readonly StyleObservable<double> _size = new();
    private readonly StyleObservable<FontWeight> _weight = new();
    private readonly StyleObservable<FontStyle> _style = new();
    private readonly StyleObservable<double>? _lineHeight;
    private readonly StyleObservable<double>? _letterSpacing;
    private readonly StyleObservable<Thickness>? _padding;
    private readonly IDisposable[] _bindings;

    public TextProperties(StyledElement e)
    {
        _bindings =
        [
            e.Bind(TextElement.ForegroundProperty, _foreground, BindingPriority.Style),
            e.Bind(TextElement.FontFamilyProperty, _family, BindingPriority.Style),
            e.Bind(TextElement.FontSizeProperty, _size, BindingPriority.Style),
            e.Bind(TextElement.FontWeightProperty, _weight, BindingPriority.Style),
            e.Bind(TextElement.FontStyleProperty, _style, BindingPriority.Style),
        ];

        // An AdwLabel binds these itself.
        if (e is TextBlock and not Presenters.AdwLabel)
        {
            _lineHeight = new();
            _letterSpacing = new();
            _padding = new();
            _bindings =
            [
                .. _bindings,
                e.Bind(TextBlock.LineHeightProperty, _lineHeight, BindingPriority.Style),
                e.Bind(TextBlock.LetterSpacingProperty, _letterSpacing, BindingPriority.Style),
                e.Bind(TextBlock.PaddingProperty, _padding, BindingPriority.Style),
            ];
        }
    }

    public void Push(ComputedStyle style)
    {
        _foreground.Push(CssConvert.Brush(style.Drawn(style.Core.Color)));
        _family.Push(CssConvert.FontFamily(style.Font.Family));
        _size.Push(style.Core.FontSize);
        _weight.Push(CssConvert.FontWeight(style.Font.Weight));
        _style.Push(CssConvert.FontStyle(style.Font.Style));
        if (_padding is not null)
        {
            _lineHeight!.Push(style.Font.LineHeight.Resolve(style.Core.FontSize) ?? double.NaN);
            _letterSpacing!.Push(style.Font.LetterSpacing);
            var g = new Presenters.CssBoxGeometry(style, default);
            _padding.Push(new Thickness(
                g.Margin.Left + g.Border.Left + g.Padding.Left,
                g.Margin.Top + g.Border.Top + g.Padding.Top,
                g.Margin.Right + g.Border.Right + g.Padding.Right,
                g.Margin.Bottom + g.Border.Bottom + g.Padding.Bottom));
        }
    }

    public void Dispose()
    {
        foreach (var b in _bindings)
        {
            b.Dispose();
        }
    }
}
