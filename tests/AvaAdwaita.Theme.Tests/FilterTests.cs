using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>An application stylesheet's text-shadow and filters, which gtk.css itself barely uses.</summary>
public class FilterTests
{
    private const string Css = """
        label.red-shadow { text-shadow: 3px 3px rgb(255 0 0); }
        label.soft-shadow { text-shadow: 0 2px 4px rgb(0 0 255 / 50%); }
        button.dropped { filter: drop-shadow(0 2px 6px rgb(0 0 0 / 40%)); }
        button.blurred { filter: blur(2px) opacity(50%); }
        """;

    private static void WithSheet(Action body)
    {
        var path = Path.Combine(Path.GetTempPath(), $"avaadwaita-filters-{Guid.NewGuid():N}.css");
        File.WriteAllText(path, Css);
        var theme = TestApplication.Instance.Theme;
        var uri = new Uri(path).AbsoluteUri;
        theme.StyleSheets.Add(uri);
        try
        {
            Dispatcher.UIThread.RunJobs();
            body();
        }
        finally
        {
            theme.StyleSheets.Remove(uri);
            Dispatcher.UIThread.RunJobs();
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void Text_Shadow_Draws_Under_The_Text()
    {
        WithSheet(() =>
        {
            var label = new Label { Content = "Shadow", Classes = { "red-shadow", "title-1" } };
            var w = Shots.Show(label);
            Shots.Pump(w);
            using var frame = w.CaptureRenderedFrame()!;
            Assert.True(CountReddish(frame) > 20, "no red shadow pixels");
            Assert.Null(EffectOf(label));

            label.Classes.Remove("red-shadow");
            label.Classes.Add("soft-shadow");
            Shots.Pump(w);
            var effect = Assert.IsAssignableFrom<IDropShadowEffect>(EffectOf(label));
            Assert.Equal(2, effect.OffsetY);
            Assert.Equal(Color.FromArgb(128, 0, 0, 255), effect.Color);
            w.Close();
        });
    }

    [AvaloniaFact]
    public void Drop_Shadow_And_Blur_Filters_Are_Effects()
    {
        WithSheet(() =>
        {
            var dropped = new Button { Content = "Dropped", Classes = { "dropped" } };
            var blurred = new Button { Content = "Blurred", Classes = { "blurred" } };
            var w = Shots.Show(new StackPanel { Spacing = 12, Margin = new Thickness(24), Children = { dropped, blurred } });
            Shots.Pump(w);
            var shadow = Assert.IsAssignableFrom<IDropShadowEffect>(EffectOf(dropped));
            Assert.Equal(2, shadow.OffsetY);

            // Avalonia blurs a radius r with a standard deviation of 0.288675 r + 0.5; GTK's is 3 px for a 6 px blur.
            Assert.Equal(3, (0.288675 * shadow.BlurRadius) + 0.5, 3);
            var blur = Assert.IsAssignableFrom<IBlurEffect>(EffectOf(blurred));
            Assert.Equal(2, (0.288675 * blur.Radius) + 0.5, 3);
            Assert.Contains(blurred.GetVisualDescendants(), v => v.OpacityMask is not null);
            w.Close();
        });
    }

    // The node's effects go on the presenter that draws it, inside the control's template.
    private static IEffect? EffectOf(Control c) => new Visual[] { c }.Concat(c.GetVisualDescendants()).Select(v => v.Effect).FirstOrDefault(e => e is not null);

    private static int CountReddish(Bitmap b)
    {
        var bytes = new byte[b.PixelSize.Width * b.PixelSize.Height * 4];
        var h = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            b.CopyPixels(new PixelRect(b.PixelSize), h.AddrOfPinnedObject(), bytes.Length, b.PixelSize.Width * 4);
        }
        finally
        {
            h.Free();
        }

        var rgba = b.Format == PixelFormats.Rgba8888;
        var n = 0;
        for (var i = 0; i < bytes.Length; i += 4)
        {
            var (r, g, bl) = rgba ? (bytes[i], bytes[i + 1], bytes[i + 2]) : (bytes[i + 2], bytes[i + 1], bytes[i]);
            if (r > 200 && g < 80 && bl < 80)
            {
                n++;
            }
        }

        return n;
    }
}
