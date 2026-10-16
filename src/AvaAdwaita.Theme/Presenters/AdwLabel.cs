using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.Markup;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A text node: its color, font, <c>line-height</c>, <c>letter-spacing</c>, <c>text-decoration</c> and
/// <c>font-variant-numeric</c> come from the node's computed style.
/// </summary>
public class AdwLabel : TextBlock
{
    /// <summary>Defines <see cref="RecognizesAccessKey"/>.</summary>
    public static readonly StyledProperty<bool> RecognizesAccessKeyProperty =
        AvaloniaProperty.Register<AdwLabel, bool>(nameof(RecognizesAccessKey));

    private Avalonia.Controls.Primitives.AccessText? _accessText;
    private int _accessKeyIndex = -1;
    private readonly StyleObservable<IBrush?> _foreground = new();
    private readonly StyleObservable<FontFamily> _family = new();
    private readonly StyleObservable<double> _size = new();
    private readonly StyleObservable<FontWeight> _weight = new();
    private readonly StyleObservable<FontStyle> _style = new();
    private readonly StyleObservable<double> _lineHeight = new();
    private readonly StyleObservable<double> _letterSpacing = new();
    private readonly StyleObservable<TextDecorationCollection?> _decorations = new();
    private readonly StyleObservable<FontFeatureCollection?> _features = new();
    private readonly StyleObservable<Thickness> _padding = new();
    private IBrush? _underline;
    private IDisposable[]? _bindings;
    private NodeEffects? _effects;

    static AdwLabel()
    {
        AffectsRender<AdwLabel>(Avalonia.Controls.Primitives.AccessText.ShowAccessKeyProperty);
        AffectsMeasure<AdwLabel>(RecognizesAccessKeyProperty);
        TextProperty.Changed.AddClassHandler<AdwLabel>((l, _) => l.UpdateAccessKey());
        RecognizesAccessKeyProperty.Changed.AddClassHandler<AdwLabel>((l, _) => l.UpdateAccessKey());
        ClipToBoundsProperty.OverrideDefaultValue<AdwLabel>(false);
        Nodes.Css.StyleProperty.Changed.AddClassHandler<AdwLabel>((l, _) => l.OnStyleChanged());
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(TextBlock);

    /// <summary>
    /// Treats the first single <c>_</c> as a mnemonic, as GtkLabel's use-underline: the marker is not shown, the next
    /// character is the access key, and it is underlined while the access keys show.
    /// </summary>
    public bool RecognizesAccessKey
    {
        get => GetValue(RecognizesAccessKeyProperty);
        set => SetValue(RecognizesAccessKeyProperty, value);
    }

    /// <inheritdoc/>
    protected override Avalonia.Media.TextFormatting.TextLayout CreateTextLayout(string? text) =>
        base.CreateTextLayout(RecognizesAccessKey ? StripMnemonic(text, out _) : text);

    // "_Open" → "Open" with the marker at 0; "__" is a literal underscore.
    private static string? StripMnemonic(string? text, out int index)
    {
        index = -1;
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new System.Text.StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '_' && i + 1 < text.Length)
            {
                if (text[i + 1] == '_')
                {
                    sb.Append('_');
                    i++;
                    continue;
                }

                if (index < 0)
                {
                    index = sb.Length;
                    continue;
                }
            }

            sb.Append(text[i]);
        }

        return sb.ToString();
    }

    // Only AccessText can register an access key, so an invisible one carries the mnemonic; a press bubbles from it
    // to the control that owns the label, as from Avalonia's own access text.
    private void UpdateAccessKey()
    {
        _accessKeyIndex = -1;
        var hasKey = RecognizesAccessKey && StripMnemonic(Text, out var index) is { } stripped && index >= 0 && index < stripped.Length;
        if (hasKey)
        {
            StripMnemonic(Text, out _accessKeyIndex);
            if (_accessText is null)
            {
                _accessText = new Avalonia.Controls.Primitives.AccessText { Opacity = 0, IsHitTestVisible = false, Width = 0, Height = 0 };
                VisualChildren.Add(_accessText);
            }

            _accessText.Text = Text;
        }
        else if (_accessText is not null)
        {
            VisualChildren.Remove(_accessText);
            _accessText = null;
        }
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        _accessText?.Arrange(default);
        return size;
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _bindings =
        [
            this.Bind(ForegroundProperty, _foreground, BindingPriority.Style),
            this.Bind(FontFamilyProperty, _family, BindingPriority.Style),
            this.Bind(FontSizeProperty, _size, BindingPriority.Style),
            this.Bind(FontWeightProperty, _weight, BindingPriority.Style),
            this.Bind(FontStyleProperty, _style, BindingPriority.Style),
            this.Bind(LineHeightProperty, _lineHeight, BindingPriority.Style),
            this.Bind(LetterSpacingProperty, _letterSpacing, BindingPriority.Style),
            this.Bind(TextDecorationsProperty, _decorations, BindingPriority.Style),
            this.Bind(FontFeaturesProperty, _features, BindingPriority.Style),
            this.Bind(PaddingProperty, _padding, BindingPriority.Style),
        ];
        _effects = new NodeEffects(this);
        OnStyleChanged();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_bindings is not null)
        {
            foreach (var b in _bindings)
            {
                b.Dispose();
            }
        }

        _bindings = null;
        _effects?.Dispose();
        _effects = null;
    }

    private void OnStyleChanged()
    {
        if (GetValue(Nodes.Css.StyleProperty) is not { } s || _bindings is null)
        {
            return;
        }

        _foreground.Push(CssConvert.Brush(s.Drawn(s.Core.Color)));
        _family.Push(CssConvert.FontFamily(s.Font.Family));
        _size.Push(s.Core.FontSize);
        _weight.Push(CssConvert.FontWeight(s.Font.Weight));
        _style.Push(CssConvert.FontStyle(s.Font.Style));
        _lineHeight.Push(s.Font.LineHeight.Resolve(s.Core.FontSize) ?? double.NaN);
        _letterSpacing.Push(s.Font.LetterSpacing);
        _decorations.Push(Decorations(s));
        _underline = s.TextDecoration.Line.Split(' ').Contains("underline") ? CssConvert.Brush(s.Drawn(s.TextDecoration.Color)) : null;
        _features.Push(CssConvert.Features(s.FontVariant.Numeric));

        // A blurred text shadow can only be the text visual's effect; on a label node it shadows the box too.
        var shadow = s.Font.TextShadow.FirstOrDefault(t => t.Blur > 0 && !t.Inset) is { } blurred ? NodeEffects.DropShadow(blurred) : null;
        if (Nodes.Css.GetCssNode(this) is null)
        {
            _effects?.PushEffect(shadow);
        }
        else
        {
            _effects?.Push(s, shadow);

            // A label node's margin, border and padding surround the text.
            var g = new CssBoxGeometry(s, default);
            _padding.Push(new Thickness(
                g.Margin.Left + g.Border.Left + g.Padding.Left,
                g.Margin.Top + g.Border.Top + g.Padding.Top,
                g.Margin.Right + g.Border.Right + g.Padding.Right,
                g.Margin.Bottom + g.Border.Bottom + g.Padding.Bottom));
            InvalidateVisual();
        }
    }

    /// <inheritdoc/>
    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        // A label taller than its text centers it, as GtkLabel's default yalign of 0.5.
        var extra = Bounds.Height - Padding.Top - Padding.Bottom - TextLayout.Height;
        if (extra > 0)
        {
            origin = origin.WithY(origin.Y + Math.Floor(extra / 2));
        }

        // GtkLabel places a centered or end-aligned line at a whole pixel (floor of xalign × the spare width).
        if (TextLayout.TextLines.Count == 1 && TextLayout.TextLines[0].Start is var start && start != Math.Floor(start))
        {
            origin = origin.WithX(origin.X - (start - Math.Floor(start)));
        }

        if (Nodes.Css.GetCssNode(this) is not null && GetValue(Nodes.Css.StyleProperty) is { } s)
        {
            BoxPainter.Paint(context, s, Bounds.Size, this);
        }

        DrawTextShadows(context, origin);
        if (DrawRecorder.For(this) is { } rec && !string.IsNullOrEmpty(Text) && Foreground is ISolidColorBrush fg)
        {
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight);
            var family = FontManager.Current.TryGetGlyphTypeface(typeface, out var face)
                ? FontFamily.FamilyNames.FirstOrDefault(n => face.FamilyName.StartsWith(n, StringComparison.Ordinal)) ?? face.FamilyName
                : FontFamily.Name;
            rec.Add(this, new DrawRecord("text", new Rect(origin, new Size(TextLayout.WidthIncludingTrailingWhitespace, TextLayout.Height)))
            {
                Color = Color.FromArgb((byte)Math.Round(fg.Color.A * fg.Opacity), fg.Color.R, fg.Color.G, fg.Color.B),

                // The text's color is the node's; its unrounded alpha fades as GTK's float color does.
                Alpha = GetValue(Nodes.Css.StyleProperty) is { } ts && CssConvert.Color(ts.Drawn(ts.Core.Color)) == fg.Color && fg.Opacity >= 1 ? ts.Drawn(ts.Core.Color).Alpha : null,
                Family = family,
                Weight = (int)FontWeight,
                Size = FontSize,
            });
        }

        base.RenderTextLayout(context, origin);
        DrawUnderline(context, origin);
        if (_accessKeyIndex >= 0 && GetValue(Avalonia.Controls.Primitives.AccessText.ShowAccessKeyProperty) && Foreground is { } keyBrush)
        {
            var r = TextLayout.HitTestTextPosition(_accessKeyIndex);
            var y = Math.Round(origin.Y + r.Bottom, MidpointRounding.AwayFromZero) - 1.5;
            context.DrawLine(new Pen(keyBrush, 1), new Point(Math.Round(origin.X + r.Left), y), new Point(Math.Round(origin.X + r.Right), y));
        }
    }

    // GTK draws text-shadow under the text, the last shadow lowest; unblurred ones are the text again, offset and
    // in the shadow's color.
    private void DrawTextShadows(DrawingContext context, Point origin)
    {
        if (GetValue(Nodes.Css.StyleProperty) is not { } s || s.Font.TextShadow.Count == 0 || string.IsNullOrEmpty(Text))
        {
            return;
        }

        var text = RecognizesAccessKey ? StripMnemonic(Text, out _) : Text;
        for (var i = s.Font.TextShadow.Count - 1; i >= 0; i--)
        {
            var sh = s.Font.TextShadow[i];
            if (sh.Blur > 0 || sh.Inset)
            {
                continue;
            }

            using var layout = new Avalonia.Media.TextFormatting.TextLayout(
                text, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, new Avalonia.Media.Immutable.ImmutableSolidColorBrush(CssConvert.Color(s.Drawn(sh.Color))),
                TextAlignment, TextWrapping, TextTrimming, null, FlowDirection, TextLayout.MaxWidth, TextLayout.MaxHeight, LineHeight, LetterSpacing, MaxLines, FontFeatures, null);
            layout.Draw(context, origin + new Vector(sh.X, sh.Y));
        }
    }

    // Pango fills the underline as a rectangle whose top is the font's underline position below the baseline;
    // Avalonia's own underline centers on it, half a stroke higher.
    private void DrawUnderline(DrawingContext context, Point origin)
    {
        if (_underline is not { } brush || string.IsNullOrEmpty(Text)
            || !FontManager.Current.TryGetGlyphTypeface(new Typeface(FontFamily, FontStyle, FontWeight), out var face))
        {
            return;
        }

        var scale = FontSize / face.Metrics.DesignEmHeight;
        var rec = DrawRecorder.For(this);
        var y = origin.Y;
        foreach (var line in TextLayout.TextLines)
        {
            var rect = new Rect(origin.X + line.Start, y + line.Baseline + (face.Metrics.UnderlinePosition * scale), line.Width, Math.Max(1, face.Metrics.UnderlineThickness * scale));
            context.DrawRectangle(brush, null, rect);
            if (rec is not null && brush is ISolidColorBrush b)
            {
                rec.Add(this, new DrawRecord("color", rect) { Color = Color.FromArgb((byte)Math.Round(b.Color.A * b.Opacity), b.Color.R, b.Color.G, b.Color.B) });
            }

            y += line.Height;
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        if (Nodes.Css.GetCssNode(this) is not null && GetValue(Nodes.Css.StyleProperty) is { } s)
        {
            var p = Padding;
            size = new Size(Math.Max(size.Width, Math.Ceiling(s.Size.MinWidth) + p.Left + p.Right), Math.Max(size.Height, Math.Ceiling(s.Size.MinHeight) + p.Top + p.Bottom));
        }

        return size;
    }

    private static TextDecorationCollection? Decorations(ComputedStyle s)
    {
        var line = s.TextDecoration.Line;
        if (line == "none")
        {
            return null;
        }

        var c = new TextDecorationCollection();
        foreach (var w in line.Split(' '))
        {
            var loc = w switch
            {
                "overline" => TextDecorationLocation.Overline,
                "line-through" => TextDecorationLocation.Strikethrough,
                _ => (TextDecorationLocation?)null,
            };
            if (loc is { } l)
            {
                c.Add(new TextDecoration { Location = l, Stroke = CssConvert.Brush(s.Drawn(s.TextDecoration.Color)) });
            }
        }

        return c;
    }
}
