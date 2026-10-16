using System.IO;
using System.Linq;
using AvaAdwaita.Icons;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Every symbolic icon the theme bundles and, on Linux, every one of the installed Adwaita icon theme reads completely.</summary>
public class SymbolicIconTests
{
    private const string Installed = "/usr/share/icons/Adwaita/symbolic";

    [AvaloniaFact]
    public void Installed_Adwaita_Symbolic_Icons_Read_Completely()
    {
        if (!Directory.Exists(Installed))
        {
            Assert.Skip("adwaita-icon-theme is not installed");
        }

        var files = Directory.GetFiles(Installed, "*-symbolic.svg", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        var problems = files
            .Select(f => (File: Path.GetRelativePath(Installed, f), Svg: SymbolicSvg.Parse(File.ReadAllBytes(f))))
            .Where(x => x.Svg is null || x.Svg.Unsupported.Count > 0 || x.Svg.Shapes.Count == 0)
            .Select(x => x.Svg is null ? $"{x.File}: unreadable" : x.Svg.Shapes.Count == 0 ? $"{x.File}: no shapes" : $"{x.File}: {string.Join(", ", x.Svg.Unsupported.Distinct())}")
            .ToList();
        Assert.True(problems.Count == 0, $"{problems.Count} of {files.Length} icons:\n" + string.Join('\n', problems.Take(40)));
    }

    [AvaloniaFact]
    public void Bundled_Symbolic_Icons_Read_Completely()
    {
        var asm = typeof(AdwaitaTheme).Assembly;
        var names = asm.GetManifestResourceNames().Where(n => n.StartsWith("AvaAdwaita.Icons.", System.StringComparison.Ordinal) && n.EndsWith(".svg", System.StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(names);
        foreach (var n in names)
        {
            using var s = asm.GetManifestResourceStream(n)!;
            using var m = new MemoryStream();
            s.CopyTo(m);
            var svg = SymbolicSvg.Parse(m.ToArray());
            Assert.True(svg is { Unsupported.Count: 0 } && svg.Shapes.Count > 0, $"{n}: {(svg is null ? "unreadable" : string.Join(", ", svg.Unsupported))}");
        }
    }
}
