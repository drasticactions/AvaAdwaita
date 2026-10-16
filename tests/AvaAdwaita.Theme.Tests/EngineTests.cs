using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Selectors;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class EngineTests
{
    [Fact]
    public void Window_Background_Follows_The_Color_Scheme()
    {
        var h = new EngineHarness();
        Assert.Equal("rgb(250,250,251)", h.Value(h.Window, "background-color"));
        Assert.Equal("rgba(0,0,6,0.8)", h.Value(h.Window, "color"));
        h.Engine.Media = new CssMediaState(true, CssContrast.NoPreference, false);
        Assert.Equal("rgb(34,34,38)", h.Value(h.Window, "background-color"));
        Assert.Equal("rgb(255,255,255)", h.Value(h.Window, "color"));
    }

    [Fact]
    public void Suggested_Action_Uses_The_Accent()
    {
        var h = new EngineHarness();
        var b = h.Add(h.Window, "button", "suggested-action", "text-button");
        var l = h.Add(b, "label");
        Assert.Equal("rgb(53,132,228)", h.Value(b, "background-color"));
        Assert.Equal("rgb(255,255,255)", h.Value(l, "color"));
    }

    [Fact]
    public void Plain_Button_Hover_Changes_Background()
    {
        var h = new EngineHarness();
        var b = h.Add(h.Window, "button", "text-button");
        var normal = h.Value(b, "background-color");
        b.States |= CssStates.Hover;
        var hover = h.Value(b, "background-color");
        Assert.NotEqual(normal, hover);
        Assert.Equal("9", h.Value(b, "border-top-left-radius").Split(' ')[0].Replace("px", string.Empty));
    }
}
