using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaAdwaita.Nodes;
using AvaAdwaita.Settings;
using Tmds.DBus.Protocol;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>The settings portal, served by a fake portal on a private bus.</summary>
public class PortalTests
{
    private const string Appearance = "org.freedesktop.appearance";

    private static readonly ((string, string), VariantValue)[] s_initial =
    [
        ((Appearance, "color-scheme"), VariantValue.UInt32(1)),
        ((Appearance, "accent-color"), VariantValue.Struct(VariantValue.Double(0.5686), VariantValue.Double(0.2549), VariantValue.Double(0.6745))),
        ((Appearance, "contrast"), VariantValue.UInt32(0)),
        (("org.gnome.desktop.interface", "font-name"), VariantValue.String("Adwaita Sans 11")),
        (("org.gnome.desktop.wm.preferences", "button-layout"), VariantValue.String("close,minimize:")),
    ];

    [Fact]
    public async Task Portal_Settings_Are_Read_And_Followed()
    {
        await using var portal = await FakePortal.StartAsync(s_initial);
        Assert.SkipWhen(portal is null, "dbus-daemon is not installed");
        using var client = new DBusConnection(portal!.Address);
        await client.ConnectAsync();
        using var settings = await PortalSettings.TryCreateAsync(client);
        Assert.NotNull(settings);
        Assert.Equal(2u, settings!.Version);
        var v = settings.Current;
        Assert.Equal(SystemColorScheme.PreferDark, v.ColorScheme);
        Assert.Equal(false, v.HighContrast);
        Assert.Equal("Adwaita Sans 11", v.FontName);
        Assert.Equal("close,minimize:", v.ButtonLayout);
        Assert.Equal(AdwAccentColor.Purple, AccentColors.Nearest(v.AccentRgb!.Value.R, v.AccentRgb.Value.G, v.AccentRgb.Value.B));

        var changed = new TaskCompletionSource();
        settings.Changed += (_, _) => changed.TrySetResult();
        portal.Set(Appearance, "contrast", VariantValue.UInt32(1));
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(true, settings.Current.HighContrast);
    }

    [AvaloniaFact]
    public void A_Portal_Change_Restyles_Within_One_Pass()
    {
        // The bus calls must not resume on the UI thread, which this test blocks while it waits.
        var portal = Task.Run(() => FakePortal.StartAsync(s_initial)).GetAwaiter().GetResult();
        Assert.SkipWhen(portal is null, "dbus-daemon is not installed");
        using var client = new DBusConnection(portal!.Address);
        Task.Run(() => client.ConnectAsync().AsTask()).GetAwaiter().GetResult();
        var settings = Task.Run(() => PortalSettings.TryCreateAsync(client)).GetAwaiter().GetResult()!;
        var theme = TestApplication.Instance.Theme;
        var state = AdwaitaEnvironment.State;
        state.AddSource(settings);
        var button = new Button { Content = "Button" };
        button.Classes.Add("suggested-action");
        var w = Shots.Show(button);
        AdwaitaEnvironment.SuppressTransitions(w);
        try
        {
            Assert.True(state.Dark);
            Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
            var before = Css.GetCssNode(button)!.Style!.Background.Color;

            var changed = new System.Threading.ManualResetEventSlim();
            settings.Changed += (_, _) => changed.Set();
            portal.Set(Appearance, "color-scheme", VariantValue.UInt32(2));
            portal.Set(Appearance, "accent-color", VariantValue.Struct(VariantValue.Double(0.2275), VariantValue.Double(0.5804), VariantValue.Double(0.2902)));
            Assert.True(changed.Wait(TimeSpan.FromSeconds(5)));
            System.Threading.Thread.Sleep(200);

            Dispatcher.UIThread.RunJobs();
            Assert.False(state.Dark);
            Assert.Equal(AdwAccentColor.Green, state.Accent);
            Assert.Equal(ThemeVariant.Light, Application.Current!.RequestedThemeVariant);
            Assert.NotEqual(before, Css.GetCssNode(button)!.Style!.Background.Color);
        }
        finally
        {
            state.RemoveSource(settings);
            settings.Dispose();
            AdwaitaEnvironment.ResumeTransitions(w);
            w.Close();
            theme.ColorScheme = AdwColorScheme.Default;
            Task.Run(() => portal.DisposeAsync().AsTask()).GetAwaiter().GetResult();
        }
    }
}
