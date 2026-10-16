using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Values;
using AvaAdwaita.Icons;
using AvaAdwaita.Markup;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Draws an icon node: <see cref="IconName"/>, or the node's <c>-gtk-icon-source</c>, at <c>-gtk-icon-size</c>,
/// recolored with <c>color</c> and <c>-gtk-icon-palette</c>, with <c>-gtk-icon-shadow</c> and <c>-gtk-icon-transform</c>.
/// </summary>
public class AdwIcon : Control
{
    /// <summary>Defines <see cref="IconName"/>.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<AdwIcon, string?>(nameof(IconName));

    private NodeEffects? _effects;
    private ComputedStyle? _style;
    private IconData? _icon;
    private bool _iconDirty = true;

    static AdwIcon()
    {
        AffectsRender<AdwIcon>(IconNameProperty);
        AffectsMeasure<AdwIcon>(IconNameProperty);
        IconNameProperty.Changed.AddClassHandler<AdwIcon>((i, _) => i._iconDirty = true);
        Nodes.Css.StyleProperty.Changed.AddClassHandler<AdwIcon>((i, _) => i.OnStyleChanged());
        FlowDirectionProperty.Changed.AddClassHandler<AdwIcon>((i, _) =>
        {
            i._iconDirty = true;
            i.InvalidateVisual();
        });
    }

    /// <summary>Creates an icon. Unless told otherwise it is an <c>image</c> node, as a GtkImage.</summary>
    public AdwIcon()
    {
        SetValue(Nodes.Css.NodeProperty, "image", Avalonia.Data.BindingPriority.Style);
    }

    /// <summary>A freedesktop icon name. Null uses the node's <c>-gtk-icon-source</c>.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>The icon size in px, from <c>-gtk-icon-size</c>.</summary>
    public double IconSize => _style?.Icon.Size ?? 16;

    /// <summary>The icon that was found, for diagnostics.</summary>
    public IconData? Icon
    {
        get
        {
            EnsureIcon();
            return _icon;
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _effects = new NodeEffects(this);
        IconProviders.Changed += OnProvidersChanged;
        OnStyleChanged();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        IconProviders.Changed -= OnProvidersChanged;
        _effects?.Dispose();
        _effects = null;
    }

    private void OnProvidersChanged(object? sender, EventArgs e)
    {
        _iconDirty = true;
        InvalidateVisual();
    }

    private void OnStyleChanged()
    {
        var old = _style;
        _style = GetValue(Nodes.Css.StyleProperty);
        if (_style is null)
        {
            return;
        }

        if (Nodes.Css.GetCssNode(this) is not null)
        {
            _effects?.Push(_style);
        }

        if (old is null || !Equals(old.Other.IconSource, _style.Other.IconSource) || old.Icon.Size != _style.Icon.Size)
        {
            _iconDirty = true;
            InvalidateMeasure();
        }

        InvalidateVisual();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Ceiling(IconSize);
        if (_style is not { } s || Nodes.Css.GetCssNode(this) is null)
        {
            return new Size(size, size);
        }

        // An image node has its own box: min-size, padding, border and margin around the icon.
        var g = new CssBoxGeometry(s, default);
        var extraW = g.Margin.Horizontal + g.Border.Horizontal + g.Padding.Horizontal;
        var extraH = g.Margin.Vertical + g.Border.Vertical + g.Padding.Vertical;
        return new Size(Math.Max(size, Math.Ceiling(s.Size.MinWidth)) + extraW, Math.Max(size, Math.Ceiling(s.Size.MinHeight)) + extraH);
    }

    private void EnsureIcon()
    {
        if (!_iconDirty)
        {
            return;
        }

        _iconDirty = false;
        var size = (int)Math.Ceiling(IconSize);
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        if (IconName is { } name)
        {
            _icon = IconProviders.Lookup(name, size, rtl);
            return;
        }

        // GTK looks a style sheet's -gtk-icontheme() up with no direction (gtkcssimageicontheme.c), so with no -rtl
        // variant; only an image's icon name follows the widget's direction.
        _icon = _style?.Other.IconSource switch
        {
            CssIconThemeImage it => IconProviders.Lookup(it.Name, size, rtl: false),
            CssRecolorImage rc => LoadUrl(rc.Url, symbolic: true),
            CssUrlImage u => LoadUrl(u.Url, symbolic: u.Url.EndsWith("-symbolic.svg", StringComparison.Ordinal)),
            _ => null,
        };
    }

    private static IconData? LoadUrl(string url, bool symbolic)
    {
        var bytes = AdwaitaEnvironment.ReadAsset(url);
        if (bytes is null && url.StartsWith("file://", StringComparison.Ordinal))
        {
            var path = new Uri(url).LocalPath;
            bytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        if (bytes is null)
        {
            return null;
        }

        if (url.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            using var m = new MemoryStream(bytes);
            return new IconData(url, null, new Bitmap(m), false, url);
        }

        var svg = SymbolicSvg.Parse(bytes);
        return svg is null ? null : new IconData(url, svg, null, symbolic, url);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (_style is not { } s)
        {
            return;
        }

        var content = new Rect(Bounds.Size);
        if (Nodes.Css.GetCssNode(this) is not null)
        {
            BoxPainter.Paint(context, s, Bounds.Size, this);
            content = new CssBoxGeometry(s, Bounds.Size).ContentBox;
        }

        EnsureIcon();
        if (_icon is null)
        {
            return;
        }

        var size = IconSize;
        var x = content.X + Math.Round((content.Width - size) / 2);
        var y = content.Y + Math.Round((content.Height - size) / 2);
        var box = new Rect(x, y, size, size);
        var transform = NodeEffects.Transform(s.Other.IconTransform);

        // An icon from the node's -gtk-icon-source is recolored as GTK's icon theme image does.
        var foreground = IconName is null ? s.IconSourceColor : null;

        // Avalonia mirrors a right-to-left subtree; GTK never mirrors an icon (it picks an -rtl variant instead),
        // so the icon is flipped back about its center. The node's box stays mirrored with the layout.
        var unmirror = FlowDirection == FlowDirection.RightToLeft
            ? Matrix.CreateTranslation(-box.Center.X, 0) * Matrix.CreateScale(-1, 1) * Matrix.CreateTranslation(box.Center.X, 0)
            : Matrix.Identity;
        using (unmirror.IsIdentity ? default(DrawingContext.PushedState?) : context.PushTransform(unmirror))
        using (transform is null ? default(DrawingContext.PushedState?) : context.PushTransform(
            Matrix.CreateTranslation(-box.Center.X, -box.Center.Y) * transform.Value * Matrix.CreateTranslation(box.Center.X, box.Center.Y)))
        {
            for (var i = s.Icon.Shadow.Count - 1; i >= 0; i--)
            {
                var sh = s.Icon.Shadow[i];
                Paint(context, _icon, box.Translate(new Vector(sh.X, sh.Y)), s, sh.Color);
            }

            Paint(context, _icon, box, s, null, foreground);
        }

        if (DrawRecorder.For(this) is { } rec)
        {
            RecordInk(rec, this, _icon, box, s, foreground, unmirror);
        }
    }

    /// <summary>Reports each filled shape's bounds and color, as GTK's fill nodes for a symbolic icon.</summary>
    private static void RecordInk(DrawRecorder rec, Visual owner, IconData icon, Rect box, ComputedStyle s, CssColor? foreground, Matrix unmirror)
    {
        if (icon.Vector is not { } svg)
        {
            rec.Add(owner, new DrawRecord("icon", box));
            return;
        }

        var vb = svg.ViewBox;
        var scale = Math.Min(box.Width / vb.Width, box.Height / vb.Height);
        var m = Matrix.CreateTranslation(-vb.X, -vb.Y) * Matrix.CreateScale(scale, scale)
            * Matrix.CreateTranslation(box.X + ((box.Width - (vb.Width * scale)) / 2), box.Y + ((box.Height - (vb.Height * scale)) / 2)) * unmirror;
        foreach (var shape in svg.Shapes)
        {
            if (ShapeBrush(shape.FillRole, shape.Fill, shape.FillOpacity, icon.IsSymbolic, s, null, foreground) is ISolidColorBrush b)
            {
                rec.Add(owner, new DrawRecord("icon", shape.Geometry.Bounds.TransformToAABB(m)) { Color = b.Color });
            }

            // A stroke's ink is the path widened by half the line, as GTK reports its stroke nodes.
            if (shape.StrokeRole != IconRole.None && ShapeBrush(shape.StrokeRole, shape.Stroke, shape.StrokeOpacity, icon.IsSymbolic, s, null, foreground) is ISolidColorBrush sb)
            {
                rec.Add(owner, new DrawRecord("icon", shape.Geometry.Bounds.Inflate(shape.StrokeWidth / 2).TransformToAABB(m)) { Color = sb.Color });
            }
        }
    }

    /// <summary>Draws an icon into a box, recolored from <paramref name="s"/>, or all in <paramref name="only"/> for a shadow.</summary>
    internal static void Paint(DrawingContext ctx, IconData icon, Rect box, ComputedStyle s, CssColor? only, CssColor? foreground = null)
    {
        if (icon.Bitmap is { } bmp)
        {
            if (only is null)
            {
                ctx.DrawImage(s.ColorMatrix is { } filter ? FilteredBitmaps.Get(bmp, filter) : bmp, box);
            }

            return;
        }

        if (icon.Vector is not { } svg)
        {
            return;
        }

        var vb = svg.ViewBox;
        var scale = Math.Min(box.Width / vb.Width, box.Height / vb.Height);
        var m = Matrix.CreateTranslation(-vb.X, -vb.Y) * Matrix.CreateScale(scale, scale)
            * Matrix.CreateTranslation(box.X + ((box.Width - (vb.Width * scale)) / 2), box.Y + ((box.Height - (vb.Height * scale)) / 2));
        using (ctx.PushTransform(m))
        {
            foreach (var shape in svg.Shapes)
            {
                var fill = ShapeBrush(shape.FillRole, shape.Fill, shape.FillOpacity, icon.IsSymbolic, s, only, foreground);
                IPen? pen = null;
                if (shape.StrokeRole != IconRole.None && ShapeBrush(shape.StrokeRole, shape.Stroke, shape.StrokeOpacity, icon.IsSymbolic, s, only, foreground) is { } stroke)
                {
                    pen = new Pen(stroke, shape.StrokeWidth, null, shape.LineCap, shape.LineJoin);
                }

                if (fill is not null || pen is not null)
                {
                    ctx.DrawGeometry(fill, pen, shape.Geometry);
                }
            }
        }
    }

    private static IBrush? ShapeBrush(IconRole role, Color? original, double opacity, bool symbolic, ComputedStyle s, CssColor? only, CssColor? foreground)
    {
        if (role == IconRole.None)
        {
            return null;
        }

        Color c;
        if (only is { } o)
        {
            c = CssConvert.Color(s.Drawn(o));
        }
        else if (symbolic)
        {
            var fg = foreground ?? s.Core.Color;
            var palette = s.Core.IconPalette;
            var css = role switch
            {
                IconRole.Success => palette.Get("success") ?? fg,
                IconRole.Warning => palette.Get("warning") ?? fg,
                IconRole.Error => palette.Get("error") ?? fg,
                IconRole.Accent => palette.Get("accent") ?? fg,
                _ => fg,
            };
            c = CssConvert.Color(s.Drawn(css));
        }
        else
        {
            c = original ?? Colors.Black;
        }

        if (opacity < 1)
        {
            c = Color.FromArgb((byte)Math.Round(c.A * Math.Clamp(opacity, 0, 1)), c.R, c.G, c.B);
        }

        return new Avalonia.Media.Immutable.ImmutableSolidColorBrush(c);
    }
}
