using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Theme.Tests;

/// <summary>
/// Compares each corpus scene, rendered by Avalonia with Skia, with GTK's PNG of it (tests/Data/goldens). Text
/// is rasterized differently by Pango and Skia, so GTK's text areas are masked; elsewhere at most 0.5 % of
/// pixels may differ by more than 8/255 in a channel. Cairo computes exact coverage and Skia's raster scan
/// converter supersamples, so along edges (where GTK's image changes sharply) a pixel may differ by up to
/// 40/255, and a pixel counts as different only when neither image has a value within tolerance one pixel away. A failing scene writes GTK | AvaAdwaita | difference
/// to artifacts/goldens with an index.html. Known failures are listed in goldens/known-differences.txt.
/// </summary>
public class GoldenTests
{
    private const int Margin = 12;
    private const int Tolerance = 8;
    private const double AllowedFraction = 0.005;

    // On an edge, where GTK's own image changes sharply between neighbors, anti-aliased coverage may differ more.
    private const int EdgeContrast = 32;
    private const int EdgeTolerance = 40;

    [AvaloniaFact]
    public void Scenes_Match_Gtk_Pixels() => Compare(1, "goldens", Path.Combine(ParityRunner.DataDir, "goldens"));

    /// <summary>The same scenes on a scale-2 output, against GTK's scale-2 renders.</summary>
    [AvaloniaFact]
    public void Scenes_Match_Gtk_Pixels_At_Scale_2() => Compare(2, Path.Combine("goldens", "scale2"), Path.Combine(ParityRunner.DataDir, "goldens", "scale2"));

    private static void Compare(int scale, string name, string dataDir)
    {
        var outDir = Path.Combine(TestData.RepoRoot, "artifacts", name);
        if (Directory.Exists(outDir))
        {
            foreach (var f in Directory.GetFiles(outDir, "*.png"))
            {
                File.Delete(f);
            }
        }

        Directory.CreateDirectory(outDir);
        var html = new StringBuilder("<!doctype html><meta charset=utf-8><title>Golden differences</title><body style=\"font:13px sans-serif\">\n");
        ParityRunner.Run(name, Path.Combine(dataDir, "known-differences.txt"), (id, entry, gtk, control, window, problems) =>
        {
            var path = Path.Combine(dataDir, id + ".png");
            if (!File.Exists(path))
            {
                return;
            }

            // A popover's surface is transparent around it; the window's background shows through on screen.
            var reference = Pixels.Load(path);
            var opaque = reference.Opaque();
            reference.Over(CssConvertBackground(window));
            // A spinner turns: freeze ours at the moment GTK's image shows.
            if (control is AdwSpinner spinner)
            {
                spinner.FixedTime = SpinnerTime(reference, scale, CssConvertBackground(window));
                spinner.InvalidateVisual();
                ParityRunner.Settle();
            }

            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame");
            var full = Pixels.From(frame);
            var style = control.GetValue(Css.StyleProperty)!;
            var border = new CssBoxGeometry(style, control.Bounds.Size).BorderBox.TransformToAABB(control.TransformToVisual(window) ?? Matrix.Identity);
            var origin = new PixelPoint((int)Math.Round((border.X - Margin) * scale), (int)Math.Round((border.Y - Margin) * scale));
            var actual = full.Crop(origin.X, origin.Y, reference.Width, reference.Height);

            var mask = new bool[reference.Width * reference.Height];

            // The window's own header bar reaches the window's edges, as a dialog's bottom sheet reaches its bottom, where
            // the margin around it and the corners outside its rounding are outside the window: GTK renders nothing
            // there and the screen shows the desktop. Only the window's own pixels compare, less the window's 1px
            // outline (window.csd's outline, inset), which GTK draws over the scene's edge.
            if ((string?)entry["widget"]?["type"] == "headerbar" || (bool?)entry["widget"]?["sheet"] == true)
            {
                var inner = new Rect(Margin * scale, Margin * scale, reference.Width - (2 * Margin * scale), reference.Height - (2 * Margin * scale)).Deflate(scale);
                for (var i = 0; i < mask.Length; i++)
                {
                    mask[i] = !inner.Contains(new Point(i % reference.Width, i / reference.Width)) || !opaque[i];
                }
            }
            foreach (var d in gtk["draws"]!.AsArray().Select(x => x!.AsObject()).Where(d => (string)d["k"]! == "text"))
            {
                var r = ParityComparer.Rect(d).Translate(new Vector(Margin, Margin)).Inflate(2) * scale;
                for (var y = Math.Max(0, (int)Math.Floor(r.Y)); y < Math.Min(reference.Height, (int)Math.Ceiling(r.Bottom)); y++)
                {
                    for (var x = Math.Max(0, (int)Math.Floor(r.X)); x < Math.Min(reference.Width, (int)Math.Ceiling(r.Right)); x++)
                    {
                        mask[(y * reference.Width) + x] = true;
                    }
                }
            }

            var compared = 0;
            var bad = 0;
            var diff = new Pixels(reference.Width, reference.Height);
            for (var i = 0; i < mask.Length; i++)
            {
                var d = reference.Distance(actual, i);
                if (d > Tolerance && (actual.NearbyMatch(reference, i, Tolerance) || reference.NearbyMatch(actual, i, Tolerance)
                    || (d <= EdgeTolerance && reference.Contrast(i) > EdgeContrast)))
                {
                    d = 0;
                }

                diff.Set(i, d > Tolerance ? (mask[i] ? 0xff808080u : 0xffff0000u) : (mask[i] ? 0xff404040u : 0xff000000u));
                if (mask[i])
                {
                    continue;
                }

                compared++;
                if (d > Tolerance)
                {
                    bad++;
                }
            }

            if (compared > 0 && bad > compared * AllowedFraction)
            {
                problems.Add(FormattableString.Invariant($"pixels: {bad} of {compared} differ by more than {Tolerance}/255 ({100.0 * bad / compared:0.##} %)"));
                Pixels.SideBySide(reference, actual, diff).Save(Path.Combine(outDir, id + ".png"));
                html.Append("<h3>").Append(id).Append("</h3><p>").Append(bad).Append(" pixels; GTK | AvaAdwaita | difference</p><img src=\"").Append(id).Append(".png\" style=\"image-rendering:pixelated;zoom:2\">\n");
            }
        }, scale);
        File.WriteAllText(Path.Combine(outDir, "index.html"), html.Append("</body>").ToString());
    }

    /// <summary>
    /// The time into the spinner's animation whose arc covers the angles where GTK's image has its arc: sampled at
    /// each degree along the line's middle, a pixel far from the background is arc and one near it the faint circle.
    /// </summary>
    private static double SpinnerTime(Pixels reference, int scale, uint background)
    {
        const int Bins = 360;
        var center = (Margin + 16) * scale;
        var radius = 14 * scale;
        var arc = new bool[Bins];
        for (var i = 0; i < Bins; i++)
        {
            var a = i * Math.PI * 2 / Bins;
            var x = (int)Math.Round(center + (radius * Math.Cos(a)));
            var y = (int)Math.Round(center + (radius * Math.Sin(a)));
            arc[i] = reference.DistanceTo(x, y, background) > 100;
        }

        var best = 0.0;
        var bestScore = int.MaxValue;
        for (var t = 0.0; t < AdwSpinner.Period; t += 2)
        {
            var (start, end) = AdwSpinner.Arc(t);
            var sweep = (start - end + (Math.PI * 4)) % (Math.PI * 2);
            var score = 0;
            for (var i = 0; i < Bins && score < bestScore; i++)
            {
                var from = ((i * Math.PI * 2 / Bins) - end + (Math.PI * 4)) % (Math.PI * 2);
                if ((from <= sweep) != arc[i])
                {
                    score++;
                }
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }

    private static uint CssConvertBackground(Window window)
    {
        var c = Markup.CssConvert.Color(Css.GetCssNode(window)!.Style!.Background.Color);
        return ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
    }

    /// <summary>Straight (not premultiplied) RGBA pixels as 0xAARRGGBB.</summary>
    private sealed class Pixels(int width, int height)
    {
        private readonly uint[] _data = new uint[width * height];

        public int Width { get; } = width;

        public int Height { get; } = height;

        public static Pixels Load(string path)
        {
            using var b = new Bitmap(path);
            return From(b);
        }

        public static Pixels From(Bitmap b)
        {
            var p = new Pixels(b.PixelSize.Width, b.PixelSize.Height);
            var bytes = new byte[p.Width * p.Height * 4];
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                b.CopyPixels(new PixelRect(0, 0, p.Width, p.Height), handle.AddrOfPinnedObject(), bytes.Length, p.Width * 4);
            }
            finally
            {
                handle.Free();
            }

            var rgba = b.Format == PixelFormats.Rgba8888;
            var premul = b.AlphaFormat == AlphaFormat.Premul;
            for (var i = 0; i < p._data.Length; i++)
            {
                uint c0 = bytes[i * 4], c1 = bytes[(i * 4) + 1], c2 = bytes[(i * 4) + 2], a = bytes[(i * 4) + 3];
                var (r, g, bl) = rgba ? (c0, c1, c2) : (c2, c1, c0);
                if (premul && a is > 0 and < 255)
                {
                    r = Math.Min(255, (r * 255) / a);
                    g = Math.Min(255, (g * 255) / a);
                    bl = Math.Min(255, (bl * 255) / a);
                }

                p._data[i] = (a << 24) | (r << 16) | (g << 8) | bl;
            }

            return p;
        }

        public Pixels Crop(int x0, int y0, int w, int h)
        {
            var p = new Pixels(w, h);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var sx = x0 + x;
                    var sy = y0 + y;
                    p._data[(y * w) + x] = sx >= 0 && sy >= 0 && sx < Width && sy < Height ? _data[(sy * Width) + sx] : 0;
                }
            }

            return p;
        }

        public void Set(int i, uint argb) => _data[i] = argb;

        /// <summary>The largest channel difference between the pixel at (x, y) and a color.</summary>
        public int DistanceTo(int x, int y, uint argb) => x < 0 || y < 0 || x >= Width || y >= Height ? 0 : Distance(_data[(y * Width) + x], argb);

        /// <summary>Which pixels are opaque: inside the window's rounded shape, for a window's own render.</summary>
        public bool[] Opaque() => _data.Select(p => p >> 24 == 255).ToArray();

        /// <summary>Composites these pixels over an opaque color.</summary>
        public void Over(uint background)
        {
            for (var i = 0; i < _data.Length; i++)
            {
                var p = _data[i];
                var a = p >> 24;
                if (a == 255)
                {
                    continue;
                }

                uint Mix(int shift) => ((((p >> shift) & 0xff) * a) + (((background >> shift) & 0xff) * (255 - a)) + 127) / 255;
                _data[i] = 0xff000000u | (Mix(16) << 16) | (Mix(8) << 8) | Mix(0);
            }
        }

        public int Distance(Pixels other, int i) => Distance(_data[i], other._data[i]);

        private static int Distance(uint a, uint b)
        {
            var d = 0;
            for (var shift = 0; shift < 32; shift += 8)
            {
                d = Math.Max(d, Math.Abs((int)((a >> shift) & 0xff) - (int)((b >> shift) & 0xff)));
            }

            return d;
        }

        /// <summary>True when a pixel next to <paramref name="i"/> here matches <paramref name="other"/>'s pixel <paramref name="i"/>.</summary>
        public bool NearbyMatch(Pixels other, int i, int tolerance)
        {
            var x = i % Width;
            var y = i / Width;
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if ((dx != 0 || dy != 0) && nx >= 0 && ny >= 0 && nx < Width && ny < Height
                        && Distance(_data[(ny * Width) + nx], other._data[i]) <= tolerance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>The largest difference between pixel <paramref name="i"/> and its neighbors.</summary>
        public int Contrast(int i)
        {
            var x = i % Width;
            var y = i / Width;
            var c = 0;
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
                    {
                        c = Math.Max(c, Distance(_data[(ny * Width) + nx], _data[i]));
                    }
                }
            }

            return c;
        }

        public static Pixels SideBySide(params Pixels[] parts)
        {
            var w = parts.Sum(p => p.Width) + (4 * (parts.Length - 1));
            var h = parts.Max(p => p.Height);
            var o = new Pixels(w, h);
            Array.Fill(o._data, 0xffffffffu);
            var x0 = 0;
            foreach (var p in parts)
            {
                for (var y = 0; y < p.Height; y++)
                {
                    Array.Copy(p._data, y * p.Width, o._data, (y * w) + x0, p.Width);
                }

                x0 += p.Width + 4;
            }

            return o;
        }

        public void Save(string path)
        {
            using var wb = new WriteableBitmap(new PixelSize(Width, Height), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Unpremul);
            using (var fb = wb.Lock())
            {
                for (var y = 0; y < Height; y++)
                {
                    var row = new int[Width];
                    for (var x = 0; x < Width; x++)
                    {
                        row[x] = unchecked((int)_data[(y * Width) + x]);
                    }

                    Marshal.Copy(row, 0, fb.Address + (y * fb.RowBytes), Width);
                }
            }

            wb.Save(path, new PngBitmapEncoderOptions());
        }
    }
}
