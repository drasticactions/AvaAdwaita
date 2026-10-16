using System;
using System.IO;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>The opt-in user stylesheet, <c>$XDG_CONFIG_HOME/gtk-4.0/gtk.css</c>, at GTK's user priority.</summary>
public class UserStyleSheetTests
{
    [Fact]
    public void It_Is_Off_By_Default() => Assert.False(new AvaAdwaitaOptions().LoadUserStyleSheet);

    [Fact]
    public void Path_Follows_Xdg_Config_Home()
    {
        var old = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        try
        {
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", "/tmp/somewhere");
            Assert.Equal(Path.Combine("/tmp/somewhere", "gtk-4.0", "gtk.css"), AdwaitaEnvironment.UserStyleSheetPath());
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "gtk-4.0", "gtk.css"), AdwaitaEnvironment.UserStyleSheetPath());
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", old);
        }
    }

    [Fact]
    public void A_Missing_Sheet_Is_None() =>
        Assert.Null(AdwaitaEnvironment.ReadUserSheet(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "gtk.css")));

    [Fact]
    public void User_Rules_Win_Over_Application_Rules()
    {
        var dir = Directory.CreateTempSubdirectory("avaadwaita-user-css").FullName;
        try
        {
            var path = Path.Combine(dir, "gtk.css");
            File.WriteAllText(path, "button { color: red; }\n");
            var sheet = AdwaitaEnvironment.ReadUserSheet(path);
            Assert.NotNull(sheet);
            Assert.Equal(CssOrigin.User, sheet.Origin);

            // The application rule is more specific; GTK's user priority still wins.
            var h = new EngineHarness(appCss: "window button.suggested-action { color: blue; }");
            var button = h.Add(h.Window, "button", "suggested-action");
            Assert.Equal("rgb(0,0,255)", h.Value(button, "color"));
            h.Styles.Add(sheet);
            h.Engine.InvalidateAll();
            Assert.Equal("rgb(255,0,0)", h.Value(button, "color"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
