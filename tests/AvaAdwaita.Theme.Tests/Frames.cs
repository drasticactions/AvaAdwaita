using System;
using Avalonia.Headless;
using Avalonia.Threading;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Lets time pass as it does in an application, for tests that watch an animation.</summary>
public static class Frames
{
    /// <summary>
    /// Runs the dispatcher for <paramref name="milliseconds"/>, its timers included (Avalonia paces animation
    /// frames that draw nothing with one), then renders a frame.
    /// </summary>
    public static void Run(int milliseconds)
    {
        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, TimeSpan.FromMilliseconds(milliseconds));
        Dispatcher.UIThread.PushFrame(frame);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
