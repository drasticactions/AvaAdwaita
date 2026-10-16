using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Theme.Tests;

/// <summary>A style engine over the bundled gtk.css and the accent provider, for tests without Avalonia.</summary>
internal sealed class EngineHarness
{
    private static CssStyleSheet? s_gtk;

    public EngineHarness(string accent = "#3584e4", string? appCss = null)
    {
        s_gtk ??= CssStyleSheet.Parse(TestData.BundledCss, CssOrigin.Theme, "gtk.css", "resource:///org/gnome/Adwaita/styles/");
        Styles.Add(s_gtk);
        Styles.Add(CssStyleSheet.Parse($"@define-color accent_bg_color {accent};\n@define-color accent_fg_color white;\n", CssOrigin.Theme, "accent"));
        if (appCss is not null)
        {
            Styles.Add(CssStyleSheet.Parse(appCss, CssOrigin.Application, "app"));
        }

        Engine = new CssStyleEngine(Styles);
        Window = new CssNode("window");
        Window.SetClasses(["background", "csd"]);
        Window.States = CssEngine.Selectors.CssStates.DirLtr;
        Engine.AddRoot(Window);
    }

    public CssStyleSet Styles { get; } = new();

    public CssStyleEngine Engine { get; }

    public CssNode Window { get; }

    public CssNode Add(CssNode parent, string name, params string[] classes)
    {
        var n = new CssNode(name) { States = CssEngine.Selectors.CssStates.DirLtr };
        n.SetClasses(classes);
        parent.AppendChild(n);
        return n;
    }

    public CssEngine.Computed.ComputedStyle Style(CssNode n) => Engine.Resolve(n);

    public string Value(CssNode n, string property) => Engine.Resolve(n).GetValueText(property)!;
}
