using System.Collections.Generic;
using AvaAdwaita.Settings;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class SettingsTests
{
    private sealed class FakeSource(SettingsValues values) : ISettingsSource
    {
        public string Name => "fake";

        public SettingsValues Current { get; private set; } = values;

        public event System.EventHandler? Changed;

        public void Set(SettingsValues v)
        {
            Current = v;
            Changed?.Invoke(this, System.EventArgs.Empty);
        }

        public void Dispose()
        {
        }
    }

    [Theory]
    [InlineData(0.2078, 0.5176, 0.8941, AdwAccentColor.Blue)]
    [InlineData(0.1294, 0.5647, 0.6431, AdwAccentColor.Teal)]
    [InlineData(0.2275, 0.5804, 0.2902, AdwAccentColor.Green)]
    [InlineData(0.7843, 0.5333, 0.0, AdwAccentColor.Yellow)]
    [InlineData(0.9294, 0.3569, 0.0, AdwAccentColor.Orange)]
    [InlineData(0.9020, 0.1765, 0.2588, AdwAccentColor.Red)]
    [InlineData(0.8353, 0.3804, 0.6, AdwAccentColor.Pink)]
    [InlineData(0.5686, 0.2549, 0.6745, AdwAccentColor.Purple)]
    [InlineData(0.4353, 0.5137, 0.5882, AdwAccentColor.Slate)]
    [InlineData(0.5, 0.5, 0.5, AdwAccentColor.Slate)]
    [InlineData(0.0, 0.47, 0.84, AdwAccentColor.Blue)]
    public void Accent_Snaps_To_The_Nearest_Preset(double r, double g, double b, AdwAccentColor expected) =>
        Assert.Equal(expected, AccentColors.Nearest(r, g, b));

    // Expected values from libadwaita 1.10.0's adw_rgba_to_standalone.
    [Theory]
    [InlineData(AdwAccentColor.Blue, false, "rgb(4,97,190)")]
    [InlineData(AdwAccentColor.Blue, true, "rgb(129,208,255)")]
    [InlineData(AdwAccentColor.Yellow, false, "rgb(144,83,0)")]
    [InlineData(AdwAccentColor.Yellow, true, "rgb(255,192,87)")]
    [InlineData(AdwAccentColor.Slate, false, "rgb(82,102,120)")]
    [InlineData(AdwAccentColor.Slate, true, "rgb(187,209,229)")]
    public void Standalone_Accent_Matches_Libadwaita(AdwAccentColor accent, bool dark, string expected) =>
        Assert.Equal(expected, AccentColors.ToStandalone(AccentColors.ToColor(accent), dark).ToString());

    [Theory]
    [InlineData("Adwaita Sans 11", "Adwaita Sans", 11.0, false, 400)]
    [InlineData("Noto Sans  10", "Noto Sans", 10.0, false, 400)]
    [InlineData("Cantarell Bold Italic 10.5", "Cantarell", 10.5, false, 700)]
    [InlineData("Monospace 13px", "Monospace", 13.0, true, 400)]
    [InlineData("Sans", "Sans", null, false, 400)]
    public void Pango_Descriptions_Parse(string text, string family, double? size, bool absolute, int weight)
    {
        var d = PangoFontDescription.Parse(text);
        Assert.Equal(family, d.Family);
        Assert.Equal(size, d.Size);
        Assert.Equal(absolute, d.SizeIsAbsolute);
        Assert.Equal(weight, d.Weight);
    }

    [Fact]
    public void Color_Scheme_Overrides_Follow_Libadwaita()
    {
        var src = new FakeSource(new SettingsValues { ColorScheme = SystemColorScheme.PreferLight });
        var s = new StyleState();
        s.AddSource(src);
        Assert.False(s.Dark);
        s.ColorScheme = AdwColorScheme.PreferDark;
        Assert.False(s.Dark);
        src.Set(new SettingsValues { ColorScheme = SystemColorScheme.Default });
        Assert.True(s.Dark);
        s.ColorScheme = AdwColorScheme.PreferLight;
        Assert.False(s.Dark);
        src.Set(new SettingsValues { ColorScheme = SystemColorScheme.PreferDark });
        Assert.True(s.Dark);
        s.ColorScheme = AdwColorScheme.ForceLight;
        Assert.False(s.Dark);
        s.ColorScheme = AdwColorScheme.Default;
        s.RequestedDark = false;
        Assert.False(s.Dark);
    }

    [Fact]
    public void Earlier_Sources_Win_And_Changes_Are_Flagged()
    {
        var debug = new FakeSource(new SettingsValues { HighContrast = true });
        var portal = new FakeSource(new SettingsValues { HighContrast = false, AccentRgb = (0.9, 0.18, 0.26), FontName = "Adwaita Sans 11" });
        var s = new StyleState();
        s.AddSource(debug);
        s.AddSource(portal);
        Assert.True(s.HighContrast);
        Assert.Equal(AdwAccentColor.Red, s.Accent);
        Assert.Equal("Adwaita Sans", s.InterfaceFont.Family);
        Assert.Equal("Monospace", s.MonospaceFont.Family);
        var changes = new List<StyleStateChange>();
        s.Changed += (_, c) => changes.Add(c);
        portal.Set(new SettingsValues { AccentRgb = (0.2, 0.5, 0.9), FontName = "Adwaita Sans 11" });
        Assert.Equal([StyleStateChange.Accent], changes);
        Assert.Contains("@define-color accent_bg_color rgb(53,132,228);", s.AccentCss());
        Assert.Contains("--monospace-font-size: 11pt;", s.FontsCss());
    }

    [Fact]
    public void Debug_Environment_Is_Read()
    {
        var env = new Dictionary<string, string> { ["ADW_DEBUG_COLOR_SCHEME"] = "prefer-dark", ["ADW_DEBUG_HIGH_CONTRAST"] = "1", ["ADW_DEBUG_ACCENT_COLOR"] = "#e62d42" };
        var d = new DebugEnvSettings(k => env.GetValueOrDefault(k));
        Assert.Equal(SystemColorScheme.PreferDark, d.Current.ColorScheme);
        Assert.True(d.Current.HighContrast);
        Assert.NotNull(d.Current.AccentRgb);
    }

    [Fact]
    public void Dconf_Reads_Schema_Defaults_Or_User_Values()
    {
        using var d = new DconfSettings(watch: false);
        Assert.SkipWhen(d.Current.ButtonLayout is null, "no GSettings schemas on this system");
        Assert.NotNull(d.Current.FontName);
        Assert.NotNull(d.Current.ColorScheme);
    }
}
