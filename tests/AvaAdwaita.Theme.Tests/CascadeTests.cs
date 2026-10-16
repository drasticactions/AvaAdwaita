using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class CascadeTests
{
    private sealed class Tree
    {
        public Tree(string themeCss, string? appCss = null)
        {
            Styles.Add(CssStyleSheet.Parse(themeCss, CssOrigin.Theme, "theme"));
            if (appCss is not null)
            {
                Styles.Add(CssStyleSheet.Parse(appCss, CssOrigin.Application, "app"));
            }

            Engine = new CssStyleEngine(Styles);
            Root = new CssNode("window");
            Engine.AddRoot(Root);
        }

        public CssStyleSet Styles { get; } = new();

        public CssStyleEngine Engine { get; }

        public CssNode Root { get; }

        public CssNode Add(CssNode parent, string name, params string[] classes)
        {
            var n = new CssNode(name);
            n.SetClasses(classes);
            parent.AppendChild(n);
            return n;
        }

        public string Get(CssNode n, string property) => Engine.Resolve(n).GetValueText(property)!;
    }

    [Fact]
    public void Specificity_Then_Source_Order()
    {
        var t = new Tree("button.a { color: red; } button { color: blue; } .a { color: green; } button.a { color: yellow; }");
        var b = t.Add(t.Root, "button", "a");
        Assert.Equal("rgb(255,255,0)", t.Get(b, "color"));
    }

    [Fact]
    public void Application_Origin_Beats_Theme_Specificity()
    {
        var t = new Tree("window button.a.b#x { color: red; }", "button { color: blue; }");
        var b = t.Add(t.Root, "button", "a", "b");
        b.Id = "x";
        Assert.Equal("rgb(0,0,255)", t.Get(b, "color"));
    }

    [Fact]
    public void Media_Queries_Follow_The_Engine_State()
    {
        var t = new Tree("window { color: red; } @media (prefers-color-scheme: dark) { window { color: blue; } } @media (prefers-contrast: more) { window { background-color: black; } }");
        Assert.Equal("rgb(255,0,0)", t.Get(t.Root, "color"));
        t.Engine.Media = new CssMediaState(true, CssContrast.More, false);
        Assert.Equal("rgb(0,0,255)", t.Get(t.Root, "color"));
        Assert.Equal("rgb(0,0,0)", t.Get(t.Root, "background-color"));
    }

    [Fact]
    public void Var_With_Fallback_And_Cycle()
    {
        var t = new Tree(":root { --a: red; --b: var(--c); --c: var(--b); } label { color: var(--missing, var(--a)); background-color: var(--b, green); } box { color: var(--b); }");
        var l = t.Add(t.Root, "label");
        var box = t.Add(t.Root, "box");
        Assert.Equal("rgb(255,0,0)", t.Get(l, "color"));
        Assert.Equal("rgb(0,128,0)", t.Get(l, "background-color"));

        // An invalid var() makes color unset: it inherits the root's, which is the initial white.
        Assert.Equal("rgb(255,255,255)", t.Get(box, "color"));
    }

    [Fact]
    public void Custom_Properties_Inherit_And_Substitute_At_The_Use_Site()
    {
        var t = new Tree(":root { --fg: color-mix(in srgb, currentColor 50%, transparent); } window { color: black; } label { color: red; background-color: var(--fg); }");
        var l = t.Add(t.Root, "label");
        Assert.Equal("rgba(255,0,0,0.5)", t.Get(l, "background-color"));
    }

    [Fact]
    public void Shorthand_With_Var_Expands_Late()
    {
        var t = new Tree(":root { --p: 3px 6px; } box { padding: var(--p); border: 2px solid red; }");
        var b = t.Add(t.Root, "box");
        Assert.Equal("3", t.Get(b, "padding-top"));
        Assert.Equal("6", t.Get(b, "padding-left"));
        Assert.Equal("2", t.Get(b, "border-left-width"));
    }

    [Fact]
    public void Wide_Keywords_And_All()
    {
        var t = new Tree("window { color: red; font-size: 20px; padding: 5px; } box { color: blue; padding: inherit; } label { color: initial; font-size: unset; } image { all: unset; }");
        var box = t.Add(t.Root, "box");
        var label = t.Add(box, "label");
        var image = t.Add(box, "image");
        Assert.Equal("5", t.Get(box, "padding-top"));
        Assert.Equal("rgb(255,255,255)", t.Get(label, "color"));
        Assert.Equal("20", t.Get(label, "font-size"));
        Assert.Equal("rgb(0,0,255)", t.Get(image, "color"));
        Assert.Equal("0", t.Get(image, "padding-top"));
    }

    [Fact]
    public void Inherited_CurrentColor_Recomputes_With_The_Child_Color()
    {
        var t = new Tree("window { color: red; caret-color: currentColor; } label { color: blue; }");
        var l = t.Add(t.Root, "label");
        Assert.Equal("rgb(255,0,0)", t.Get(t.Root, "caret-color"));
        Assert.Equal("rgb(0,0,255)", t.Get(l, "caret-color"));
    }

    [Fact]
    public void A_Color_Mixed_From_CurrentColor_Inherits_As_Computed()
    {
        // columnview > header > button > box mixes 40 % of the button's color; the label inside inherits that
        // color, not the mix applied again to it.
        var h = new EngineHarness();
        var view = h.Add(h.Window, "columnview", "view");
        var header = h.Add(view, "header", "activatable");
        var button = h.Add(header, "button");
        var box = h.Add(button, "box", "horizontal");
        var label = h.Add(box, "label");
        var boxColor = h.Engine.Resolve(box).Core.Color;
        Assert.Equal(boxColor, h.Engine.Resolve(label).Core.Color);
        Assert.Equal(0.32f, boxColor.Alpha, 3);
    }

    [Fact]
    public void Border_Width_Is_Zero_Without_A_Style()
    {
        var t = new Tree("box { border-width: 3px; } box.s { border-style: solid; }");
        var a = t.Add(t.Root, "box");
        var b = t.Add(t.Root, "box", "s");
        Assert.Equal("0", t.Get(a, "border-top-width"));
        Assert.Equal("3", t.Get(b, "border-top-width"));
    }

    [Fact]
    public void Em_Uses_The_Node_Font_Size_And_Percent_Font_Size_The_Parent()
    {
        var t = new Tree("window { font-size: 10px; } box { font-size: 200%; padding: 1em; } label { font-size: 1.5em; }");
        var box = t.Add(t.Root, "box");
        var label = t.Add(box, "label");
        Assert.Equal("20", t.Get(box, "font-size"));
        Assert.Equal("20", t.Get(box, "padding-top"));
        Assert.Equal("30", t.Get(label, "font-size"));
    }

    [Fact]
    public void Combinators_Not_And_Positions()
    {
        var t = new Tree(
            "list > row { color: red; } list row label { color: blue; } row:first-child { background-color: green; } row:last-child { background-color: yellow; } " +
            "row + row { margin-top: 1px; } row ~ row.x { margin-bottom: 2px; } row:not(.x) { padding-top: 4px; } row:only-child { padding-left: 9px; }");
        var list = t.Add(t.Root, "list");
        var r1 = t.Add(list, "row");
        var r2 = t.Add(list, "row");
        var r3 = t.Add(list, "row", "x");
        var label = t.Add(r1, "label");
        Assert.Equal("rgb(255,0,0)", t.Get(r1, "color"));
        Assert.Equal("rgb(0,0,255)", t.Get(label, "color"));
        Assert.Equal("rgb(0,128,0)", t.Get(r1, "background-color"));
        Assert.Equal("rgb(255,255,0)", t.Get(r3, "background-color"));
        Assert.Equal("0", t.Get(r1, "margin-top"));
        Assert.Equal("1", t.Get(r2, "margin-top"));
        Assert.Equal("2", t.Get(r3, "margin-bottom"));
        Assert.Equal("4", t.Get(r2, "padding-top"));
        Assert.Equal("0", t.Get(r3, "padding-top"));

        // Hiding a row changes the others' positions, as GTK skips invisible widgets.
        r3.Visible = false;
        Assert.Equal("rgb(255,255,0)", t.Get(r2, "background-color"));
        r2.Visible = false;
        Assert.Equal("9", t.Get(r1, "padding-left"));
    }

    [Fact]
    public void States_Dir_And_Backdrop()
    {
        var t = new Tree("button:hover:not(:disabled) { color: red; } button:dir(rtl) { margin-left: 3px; } window:backdrop button { opacity: 0.5; }");
        var b = t.Add(t.Root, "button");
        b.States = CssStates.Hover;
        Assert.Equal("rgb(255,0,0)", t.Get(b, "color"));
        b.States = CssStates.Hover | CssStates.Disabled | CssStates.DirRtl;
        Assert.Equal("rgb(255,255,255)", t.Get(b, "color"));
        Assert.Equal("3", t.Get(b, "margin-left"));
        t.Root.States = CssStates.Backdrop;
        Assert.Equal("0.5", t.Get(b, "opacity"));
    }

    [Fact]
    public void Named_Colors_Resolve_Late_And_The_Last_Definition_Wins()
    {
        var t = new Tree("@define-color fg @base; @define-color base red; label { color: @fg; } @media (prefers-color-scheme: dark) { @define-color base blue; }");
        var l = t.Add(t.Root, "label");
        Assert.Equal("rgb(255,0,0)", t.Get(l, "color"));
        t.Engine.Media = new CssMediaState(true, CssContrast.NoPreference, false);
        Assert.Equal("rgb(0,0,255)", t.Get(l, "color"));
    }

    [Fact]
    public void Unknown_Names_Fall_Back_To_The_Initial_Color()
    {
        var t = new Tree("label { color: @nope; background-color: @nope; }");
        var l = t.Add(t.Root, "label");
        Assert.Equal("rgb(255,255,255)", t.Get(l, "color"));
        Assert.Equal("rgba(0,0,0,0)", t.Get(l, "background-color"));
    }

    [Fact]
    public void Parse_Errors_Are_Classified()
    {
        var s = CssStyleSheet.Parse("a { nope: 1px; color: frob(); } b:frobnicate { color: red; } @frob { } c { color: 12px; }");
        Assert.Contains(s.Errors, e => e.Kind == AvaAdwaita.CssEngine.Syntax.CssErrorKind.UnknownProperty);
        Assert.Contains(s.Errors, e => e.Kind == AvaAdwaita.CssEngine.Syntax.CssErrorKind.UnknownFunction);
        Assert.Contains(s.Errors, e => e.Kind == AvaAdwaita.CssEngine.Syntax.CssErrorKind.UnknownPseudoClass);
        Assert.Contains(s.Errors, e => e.Kind == AvaAdwaita.CssEngine.Syntax.CssErrorKind.UnknownAtRule);
        Assert.Contains(s.Errors, e => e.Kind == AvaAdwaita.CssEngine.Syntax.CssErrorKind.BadValue);
        Assert.False(s.IsClean);
    }

    [Fact]
    public void Hover_Re_Resolves_Only_The_Hovered_Subtree()
    {
        var t = new Tree("button:hover { color: red; } label { color: inherit; }");
        var b1 = t.Add(t.Root, "button");
        t.Add(b1, "label");
        var b2 = t.Add(t.Root, "button");
        t.Engine.ResolveNow();
        var before = t.Engine.Resolve(b2);
        b1.States = CssStates.Hover;
        t.Engine.ResolveNow();
        Assert.Same(before, t.Engine.Resolve(b2));
        Assert.Equal("rgb(255,0,0)", t.Get(b1.Children[0], "color"));
    }
}
