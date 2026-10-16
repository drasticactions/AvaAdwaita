using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Renders windows to PNG files under the test output for review.</summary>
internal static class Shots
{
    public static string Directory { get; } = Path.Combine(TestData.RepoRoot, "artifacts", "shots");

    public static Window Show(object content, double width = 480, double height = 320, string title = "AvaAdwaita")
    {
        var w = new Window { Width = width, Height = height, Content = content, Title = title };
        w.Show();
        Pump(w);
        return w;
    }

    public static void Pump(Window w)
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    public static string Save(Window w, string name)
    {
        Pump(w);
        System.IO.Directory.CreateDirectory(Directory);
        var path = Path.Combine(Directory, name + ".png");
        using var frame = w.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame");
        frame.Save(path, new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        return path;
    }
}
