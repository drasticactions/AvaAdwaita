using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaAdwaita.Chrome;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>The window classes for client-side and system decorations.</summary>
/// <remarks>Alone, as whether windows are client-side is process-wide in the tests.</remarks>
[Collection(nameof(PageTransitionCollection))]
public class WindowChromeTests
{
    [AvaloniaFact]
    public void A_System_Decorated_Window_Is_Ssd()
    {
        var w = Shots.Show(new Label { Content = "Window" });
        Assert.True(Css.GetCssNode(w)!.HasClass("csd"));
        w.Close();

        WindowChrome.AssumeClientSide = false;
        try
        {
            var ssd = new Window { Width = 300, Height = 200, ExtendClientAreaToDecorationsHint = false };
            ssd.Show();
            Shots.Pump(ssd);
            var node = Css.GetCssNode(ssd)!;
            Assert.True(node.HasClass("ssd"));
            Assert.False(node.HasClass("csd"));
            ssd.Close();
        }
        finally
        {
            WindowChrome.AssumeClientSide = true;
        }
    }
}
