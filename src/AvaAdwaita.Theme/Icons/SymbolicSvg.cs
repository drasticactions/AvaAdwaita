using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Avalonia;
using Avalonia.Media;

namespace AvaAdwaita.Icons;

/// <summary>
/// Reads the SVG subset icons use (<c>path</c>, <c>rect</c>, <c>circle</c>, <c>ellipse</c>, <c>line</c>,
/// <c>polygon</c>, <c>polyline</c>, <c>g</c> with <c>transform</c> and <c>opacity</c>) into shapes that draw
/// with Avalonia geometry. Symbolic icons are recolored as GTK does: the <c>success</c>, <c>warning</c>,
/// <c>error</c> and <c>accent</c> classes take the palette, everything else the foreground color.
/// </summary>
public sealed class SymbolicSvg
{
    private SymbolicSvg(Rect viewBox, List<IconShape> shapes, List<string> unsupported)
    {
        ViewBox = viewBox;
        Shapes = shapes;
        Unsupported = unsupported;
    }

    /// <summary>The coordinate box the shapes are in.</summary>
    public Rect ViewBox { get; }

    /// <summary>The shapes, in paint order.</summary>
    public IReadOnlyList<IconShape> Shapes { get; }

    /// <summary>Element names the reader skipped because it does not draw them.</summary>
    public IReadOnlyList<string> Unsupported { get; }

    /// <summary>Parses SVG bytes, or returns null when they are not SVG.</summary>
    public static SymbolicSvg? Parse(byte[] data)
    {
        try
        {
            using var s = new MemoryStream(data);
            return Parse(s);
        }
        catch (Exception e) when (e is XmlException or FormatException or InvalidOperationException)
        {
            return null;
        }
    }

    private static SymbolicSvg? Parse(Stream stream)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, IgnoreComments = true };
        using var r = XmlReader.Create(stream, settings);
        var shapes = new List<IconShape>();
        var unsupported = new List<string>();
        var stack = new Stack<State>();
        Rect? viewBox = null;
        var skipDepth = -1;
        while (r.Read())
        {
            if (r.NodeType == XmlNodeType.EndElement)
            {
                if (skipDepth >= 0 && r.Depth == skipDepth)
                {
                    skipDepth = -1;
                }
                else if (skipDepth < 0 && IsSvg(r) && r.LocalName is "g" or "svg" or "a")
                {
                    stack.Pop();
                }

                continue;
            }

            if (r.NodeType != XmlNodeType.Element || skipDepth >= 0)
            {
                continue;
            }

            if (!IsSvg(r))
            {
                if (!r.IsEmptyElement)
                {
                    skipDepth = r.Depth;
                }

                continue;
            }

            var parent = stack.Count > 0 ? stack.Peek() : State.Root;
            var name = r.LocalName;
            switch (name)
            {
                case "svg":
                    {
                        var w = Length(r.GetAttribute("width")) ?? 16;
                        var h = Length(r.GetAttribute("height")) ?? 16;
                        viewBox ??= ParseViewBox(r.GetAttribute("viewBox")) ?? new Rect(0, 0, w, h);
                        var st = parent.Apply(r);
                        if (!r.IsEmptyElement)
                        {
                            stack.Push(st);
                        }

                        break;
                    }

                case "g":
                case "a":
                    {
                        var st = parent.Apply(r);
                        if (!r.IsEmptyElement)
                        {
                            stack.Push(st);
                        }

                        break;
                    }

                case "path":
                case "rect":
                case "circle":
                case "ellipse":
                case "line":
                case "polygon":
                case "polyline":
                    {
                        var st = parent.Apply(r);
                        var geometry = Geometry(name, r);
                        if (geometry is not null && st.Visible)
                        {
                            if (st.Transform != Matrix.Identity)
                            {
                                geometry.Transform = new MatrixTransform(st.Transform);
                            }

                            shapes.Add(new IconShape(geometry, st.FillRole, st.Fill, st.FillOpacity * st.Opacity, st.StrokeRole, st.Stroke, st.StrokeOpacity * st.Opacity,
                                st.StrokeWidth, st.LineCap, st.LineJoin));
                        }

                        if (!r.IsEmptyElement)
                        {
                            skipDepth = r.Depth;
                        }

                        break;
                    }

                case "defs":
                case "title":
                case "desc":
                case "metadata":
                case "style":
                case "clipPath":
                case "mask":
                case "linearGradient":
                case "radialGradient":
                case "filter":
                    if (!r.IsEmptyElement)
                    {
                        skipDepth = r.Depth;
                    }

                    break;
                default:
                    unsupported.Add(name);
                    if (!r.IsEmptyElement)
                    {
                        skipDepth = r.Depth;
                    }

                    break;
            }
        }

        return viewBox is null ? null : new SymbolicSvg(viewBox.Value, shapes, unsupported);
    }

    private static Rect? ParseViewBox(string? v)
    {
        if (v is null)
        {
            return null;
        }

        var n = Numbers(v);
        return n.Count == 4 && n[2] > 0 && n[3] > 0 ? new Rect(n[0], n[1], n[2], n[3]) : null;
    }

    private static double? Length(string? v)
    {
        if (string.IsNullOrWhiteSpace(v))
        {
            return null;
        }

        v = v.Trim();
        if (v.EndsWith("px", StringComparison.Ordinal))
        {
            v = v[..^2];
        }

        return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static List<double> Numbers(string s)
    {
        var list = new List<double>();
        var sb = new StringBuilder();
        void Flush()
        {
            if (sb.Length > 0 && double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                list.Add(d);
            }

            sb.Clear();
        }

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c is ',' or ' ' or '\t' or '\n' or '\r')
            {
                Flush();
            }
            else if (c == '-' && sb.Length > 0 && sb[^1] is not ('e' or 'E'))
            {
                Flush();
                sb.Append(c);
            }
            else if (c == '.' && sb.ToString().Contains('.'))
            {
                Flush();
                sb.Append(c);
            }
            else
            {
                sb.Append(c);
            }
        }

        Flush();
        return list;
    }

    private static double A(XmlReader r, string name) => Length(r.GetAttribute(name)) ?? 0;

    private static Geometry? Geometry(string name, XmlReader r)
    {
        switch (name)
        {
            case "path":
                {
                    var d = r.GetAttribute("d");
                    if (string.IsNullOrWhiteSpace(d))
                    {
                        return null;
                    }

                    var g = StreamGeometry.Parse(d);
                    return g;
                }

            case "rect":
                {
                    var x = A(r, "x");
                    var y = A(r, "y");
                    var w = A(r, "width");
                    var h = A(r, "height");
                    var rx = Length(r.GetAttribute("rx"));
                    var ry = Length(r.GetAttribute("ry")) ?? rx ?? 0;
                    rx ??= ry;
                    return new RectangleGeometry(new Rect(x, y, w, h), rx.Value, ry);
                }

            case "circle":
                {
                    var rr = A(r, "r");
                    return new EllipseGeometry(new Rect(A(r, "cx") - rr, A(r, "cy") - rr, rr * 2, rr * 2));
                }

            case "ellipse":
                {
                    var rx = A(r, "rx");
                    var ry = A(r, "ry");
                    return new EllipseGeometry(new Rect(A(r, "cx") - rx, A(r, "cy") - ry, rx * 2, ry * 2));
                }

            case "line":
                {
                    var g = new StreamGeometry();
                    using (var c = g.Open())
                    {
                        c.BeginFigure(new Point(A(r, "x1"), A(r, "y1")), false);
                        c.LineTo(new Point(A(r, "x2"), A(r, "y2")));
                        c.EndFigure(false);
                    }

                    return g;
                }

            default:
                {
                    var n = Numbers(r.GetAttribute("points") ?? string.Empty);
                    if (n.Count < 4)
                    {
                        return null;
                    }

                    var g = new StreamGeometry();
                    using (var c = g.Open())
                    {
                        c.BeginFigure(new Point(n[0], n[1]), name == "polygon");
                        for (var i = 2; i + 1 < n.Count; i += 2)
                        {
                            c.LineTo(new Point(n[i], n[i + 1]));
                        }

                        c.EndFigure(name == "polygon");
                    }

                    return g;
                }
        }
    }

    /// <summary>Parses an SVG transform list.</summary>
    // GTK's own icons (its built-in switch and pan icons) leave out the SVG namespace.
    private static bool IsSvg(XmlReader r) => r.NamespaceURI is "http://www.w3.org/2000/svg" or "";

    internal static Matrix ParseTransform(string? t)
    {
        var m = Matrix.Identity;
        if (string.IsNullOrWhiteSpace(t))
        {
            return m;
        }

        var i = 0;
        while (i < t.Length)
        {
            var open = t.IndexOf('(', i);
            var close = open < 0 ? -1 : t.IndexOf(')', open);
            if (open < 0 || close < 0)
            {
                break;
            }

            var fn = t[i..open].Trim().TrimStart(',').Trim();
            var a = Numbers(t[(open + 1)..close]);
            double Arg(int k, double d = 0) => k < a.Count ? a[k] : d;
            var step = fn switch
            {
                "matrix" when a.Count == 6 => new Matrix(a[0], a[1], a[2], a[3], a[4], a[5]),
                "translate" => Matrix.CreateTranslation(Arg(0), Arg(1)),
                "scale" => Matrix.CreateScale(Arg(0, 1), Arg(1, Arg(0, 1))),
                "rotate" when a.Count >= 3 => Matrix.CreateTranslation(-a[1], -a[2]) * Matrix.CreateRotation(a[0] * Math.PI / 180) * Matrix.CreateTranslation(a[1], a[2]),
                "rotate" => Matrix.CreateRotation(Arg(0) * Math.PI / 180),
                "skewX" => Matrix.CreateSkew(Arg(0) * Math.PI / 180, 0),
                "skewY" => Matrix.CreateSkew(0, Arg(0) * Math.PI / 180),
                _ => Matrix.Identity,
            };

            // SVG lists apply right to left: the last function is closest to the shape.
            m = step * m;
            i = close + 1;
        }

        return m;
    }

    private sealed record State(Matrix Transform, double Opacity, IconRole FillRole, Color? Fill, double FillOpacity, IconRole StrokeRole, Color? Stroke,
        double StrokeOpacity, double StrokeWidth, PenLineCap LineCap, PenLineJoin LineJoin, string? Class, bool Visible)
    {
        public static State Root { get; } = new(Matrix.Identity, 1, IconRole.Foreground, Colors.Black, 1, IconRole.None, null, 1, 1, PenLineCap.Flat, PenLineJoin.Miter, null, true);

        public State Apply(XmlReader r)
        {
            var st = this with { Transform = ParseTransform(r.GetAttribute("transform")) * Transform };
            var props = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var a in new[] { "fill", "stroke", "opacity", "fill-opacity", "stroke-opacity", "stroke-width", "stroke-linecap", "stroke-linejoin", "display", "visibility" })
            {
                if (r.GetAttribute(a) is { } v)
                {
                    props[a] = v;
                }
            }

            if (r.GetAttribute("style") is { } style)
            {
                foreach (var decl in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var colon = decl.IndexOf(':');
                    if (colon > 0)
                    {
                        props[decl[..colon].Trim()] = decl[(colon + 1)..].Trim();
                    }
                }
            }

            var cls = r.GetAttribute("class");
            var classRole = RoleFromClass(cls);
            if (props.TryGetValue("opacity", out var op) && Length(op) is { } o)
            {
                st = st with { Opacity = st.Opacity * o };
            }

            if (props.TryGetValue("fill", out var fill))
            {
                var (role, color) = Paint(fill);
                st = st with { FillRole = role, Fill = color };
            }

            if (props.TryGetValue("stroke", out var stroke))
            {
                var (role, color) = Paint(stroke);
                st = st with { StrokeRole = role, Stroke = color };
            }

            if (classRole is { } cr)
            {
                // GTK's recoloring style sheet sets the fill of classed shapes; a shape without fill stays unfilled.
                if (st.FillRole != IconRole.None)
                {
                    st = st with { FillRole = cr };
                }

                if (st.StrokeRole != IconRole.None)
                {
                    st = st with { StrokeRole = cr };
                }
            }

            if (props.TryGetValue("fill-opacity", out var fo) && Length(fo) is { } fov)
            {
                st = st with { FillOpacity = fov };
            }

            if (props.TryGetValue("stroke-opacity", out var so) && Length(so) is { } sov)
            {
                st = st with { StrokeOpacity = sov };
            }

            if (props.TryGetValue("stroke-width", out var sw) && Length(sw) is { } swv)
            {
                st = st with { StrokeWidth = swv };
            }

            if (props.TryGetValue("stroke-linecap", out var lc))
            {
                st = st with { LineCap = lc switch { "round" => PenLineCap.Round, "square" => PenLineCap.Square, _ => PenLineCap.Flat } };
            }

            if (props.TryGetValue("stroke-linejoin", out var lj))
            {
                st = st with { LineJoin = lj switch { "round" => PenLineJoin.Round, "bevel" => PenLineJoin.Bevel, _ => PenLineJoin.Miter } };
            }

            if ((props.TryGetValue("display", out var disp) && disp == "none") || (props.TryGetValue("visibility", out var vis) && vis == "hidden"))
            {
                st = st with { Visible = false };
            }

            return st with { Class = cls };
        }

        private static IconRole? RoleFromClass(string? cls)
        {
            if (cls is null)
            {
                return null;
            }

            foreach (var c in cls.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                switch (c)
                {
                    case "success" or "success-fill" or "success-stroke":
                        return IconRole.Success;
                    case "warning" or "warning-fill" or "warning-stroke":
                        return IconRole.Warning;
                    case "error" or "error-fill" or "error-stroke":
                        return IconRole.Error;
                    case "accent" or "accent-fill" or "accent-stroke":
                        return IconRole.Accent;
                }
            }

            return null;
        }

        private static (IconRole Role, Color? Color) Paint(string v)
        {
            v = v.Trim();
            if (v == "none" || v == "transparent")
            {
                return (IconRole.None, null);
            }

            if (v.StartsWith("url(", StringComparison.Ordinal))
            {
                var role = v.Contains("gpa:success", StringComparison.Ordinal) ? IconRole.Success
                    : v.Contains("gpa:warning", StringComparison.Ordinal) ? IconRole.Warning
                    : v.Contains("gpa:error", StringComparison.Ordinal) ? IconRole.Error
                    : v.Contains("gpa:accent", StringComparison.Ordinal) ? IconRole.Accent
                    : IconRole.Foreground;
                return (role, Colors.Black);
            }

            return (IconRole.Foreground, Avalonia.Media.Color.TryParse(v, out var c) ? c : Colors.Black);
        }
    }
}
