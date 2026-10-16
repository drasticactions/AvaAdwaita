using System.Linq;
using AvaAdwaita.CssEngine.Cascade;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class CoverageTests
{
    [Fact]
    public void Bundled_Stylesheet_Parses_Without_Errors()
    {
        var sheet = CssStyleSheet.Parse(TestData.BundledCss, CssOrigin.Theme, "gtk.css", "resource:///org/gnome/Adwaita/styles/");
        var errors = sheet.Errors.Where(e => e.Kind != CssEngine.Syntax.CssErrorKind.Warning).Take(40).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Equal(1453, sheet.RuleCount);
        Assert.Single(sheet.Keyframes);
    }

    /// <summary>
    /// Every declaration's property is in the table, and its value prints as CSS that parses back to the same
    /// value: the specified value carries everything the declaration said.
    /// </summary>
    [Fact]
    public void Every_Declaration_Round_Trips()
    {
        var sheet = CssStyleSheet.Parse(TestData.BundledCss, CssOrigin.Theme, "gtk.css", "resource:///org/gnome/Adwaita/styles/");
        var failures = new System.Collections.Generic.List<string>();
        var checkedCount = 0;
        foreach (var d in sheet.Rules.SelectMany(r => r.Declarations))
        {
            if (d.IsCustom)
            {
                continue;
            }

            Assert.InRange(d.PropertyId, 0, CssEngine.Properties.CssPropertyTable.Count - 1);
            if (d.HasVar || d.Pending is not null || d.Value is null or CssWideKeyword)
            {
                continue;
            }

            checkedCount++;
            var prop = CssEngine.Properties.CssPropertyTable.All[d.PropertyId];
            var text = prop.Kind.Print(d.Value);
            var r = new CssEngine.Syntax.CssReader(CssEngine.Syntax.CssComponentParser.Parse(text));
            var again = prop.Kind.Parse(r, CssEngine.Properties.CssParseContext.Default);
            var back = again is null || !r.AtEnd ? null : prop.Kind.Print(again);
            if (back != text)
            {
                failures.Add($"line {d.Line}: {prop.Name}: {text} -> {back ?? "(no parse)"}");
            }
        }

        Assert.True(checkedCount > 3000, $"only {checkedCount} declarations checked");
        Assert.True(failures.Count == 0, $"{failures.Count} of {checkedCount}:\n" + string.Join('\n', failures.Distinct().Take(40)));
    }
}
