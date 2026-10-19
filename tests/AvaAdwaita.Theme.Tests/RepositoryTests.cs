using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Metadata;
using AvaAdwaita.Markup;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public partial class RepositoryTests
{
    private static string Root => TestData.RepoRoot;

    private static IEnumerable<string> SourceFiles(params string[] patterns) =>
        new[] { "src", "tests", "samples", "tools" }
            .Select(d => Path.Combine(Root, d))
            .Where(Directory.Exists)
            .SelectMany(d => patterns.SelectMany(p => Directory.EnumerateFiles(d, p, SearchOption.AllDirectories)))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    [GeneratedRegex(@"^(?:public |internal |file )?(?:sealed |static |abstract |partial |readonly |unsafe |ref )*(?:class|record struct|record|struct|enum|interface|delegate [\w<>?, \[\]]+) (\w+)", RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    [Fact]
    public void One_Top_Level_Type_Per_File()
    {
        var offenders = new List<string>();
        foreach (var file in SourceFiles("*.cs").Where(f => Path.GetRelativePath(Root, f).StartsWith("src", StringComparison.Ordinal)))
        {
            var types = TopLevelType().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value).Distinct().ToList();
            var name = Path.GetFileName(file).Split('.')[0];
            if (types.Count > 1 || (types.Count == 1 && types[0] != name))
            {
                offenders.Add($"{Path.GetRelativePath(Root, file)}: {string.Join(", ", types)}");
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    [Theory]
    [InlineData(typeof(AdwaitaTheme))]
    [InlineData(typeof(DataGrid.AdwaitaDataGridTheme))]
    [InlineData(typeof(Controls.AdwaitaControlsTheme))]
    public void Every_Public_Namespace_Is_Mapped(System.Type theme)
    {
        var assembly = theme.Assembly;
        var mapped = assembly.GetCustomAttributes<XmlnsDefinitionAttribute>()
            .Where(a => a.XmlNamespace == "https://github.com/avaadwaita")
            .Select(a => a.ClrNamespace)
            .ToHashSet();
        var namespaces = assembly.GetExportedTypes().Select(t => t.Namespace!).Where(n => n != "CompiledAvaloniaXaml").Distinct();
        var unmapped = namespaces.Where(n => !mapped.Contains(n)).ToList();
        Assert.True(unmapped.Count == 0, string.Join(", ", unmapped));
        if (theme == typeof(AdwaitaTheme))
        {
            Assert.Contains(assembly.GetCustomAttributes<XmlnsPrefixAttribute>(), a => a.Prefix == "adw");
        }
    }

    // The sibling projects are models for this repository only and are never named in it.
    private static readonly string[] s_forbidden =
        [string.Concat("Ava", "Union"), string.Concat("Ava", "Motif"), string.Concat("Ava", "Wpf"), string.Concat("Ava", "Win"),
         string.Concat("Ava", "Enyo"), string.Concat("Ava", "Platinum")];

    [Fact]
    public void Sibling_Projects_Are_Never_Named()
    {
        var offenders = new List<string>();
        var files = SourceFiles("*.cs", "*.axaml", "*.csproj", "*.props", "*.targets", "*.sh", "*.yml", "*.yaml", "*.md", "*.json", "*.c", "*.h", "*.txt", "*.py")
            .Concat(Directory.EnumerateFiles(Root, "*.md"))
            .Concat(Directory.EnumerateFiles(Root, "*.props"))
            .Concat(Directory.Exists(Path.Combine(Root, ".github")) ? Directory.EnumerateFiles(Path.Combine(Root, ".github"), "*", SearchOption.AllDirectories) : [])
            .Concat(Directory.Exists(Path.Combine(Root, "docs")) ? Directory.EnumerateFiles(Path.Combine(Root, "docs"), "*", SearchOption.AllDirectories) : []);
        foreach (var file in files.Distinct())
        {
            var text = File.ReadAllText(file);
            foreach (var name in s_forbidden)
            {
                // A short name can sit inside a longer word, so require word boundaries.
                if (Regex.IsMatch(text, $@"\b{name}\b", RegexOptions.IgnoreCase))
                {
                    offenders.Add($"{Path.GetRelativePath(Root, file)}: {name}");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    [Theory]
    [InlineData("AvaAdwaita.Theme")]
    [InlineData("AvaAdwaita.DataGrid")]
    [InlineData("AvaAdwaita.Controls")]
    public void Every_Control_Theme_File_Is_Merged(string project)
    {
        var merged = File.ReadAllText(Path.Combine(Root, $"src/{project}/obj/ControlThemes/_Controls.axaml"));
        var missing = Directory.EnumerateFiles(Path.Combine(Root, $"src/{project}/Themes/Controls"), "*.axaml")
            .Select(Path.GetFileName)
            .Where(n => !merged.Contains("/Themes/Controls/" + n, StringComparison.Ordinal))
            .ToList();
        Assert.Empty(missing);
    }

    [GeneratedRegex(@"\{adw:Css ([A-Za-z.]+)")]
    private static partial Regex CssBinding();

    [Fact]
    public void Every_Css_Binding_Path_Exists()
    {
        var bad = SourceFiles("*.axaml")
            .SelectMany(f => CssBinding().Matches(File.ReadAllText(f)).Select(m => (File: f, Path: m.Groups[1].Value)))
            .Where(x => CssPaths.Get(x.Path) is null)
            .Select(x => $"{Path.GetRelativePath(Root, x.File)}: {x.Path}")
            .ToList();
        Assert.Empty(bad);
    }

    [Fact]
    public void Comments_Do_Not_Mention_The_Spec()
    {
        var offenders = SourceFiles("*.cs", "*.axaml")
            .Where(f => !f.EndsWith("RepositoryTests.cs", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select((l, i) => (f, l, i)))
            .Where(x => Regex.IsMatch(x.l, @"(//|<!--|\*).*\b(spec|interview|milestone)\b", RegexOptions.IgnoreCase))
            .Select(x => $"{Path.GetRelativePath(Root, x.f)}:{x.i + 1}")
            .ToList();
        Assert.Empty(offenders);
    }

    [Fact]
    public void Bundled_Css_Equals_Sassc_Output()
    {
        var sassc = new[] { "/usr/bin/sassc", "/usr/local/bin/sassc" }.FirstOrDefault(File.Exists);
        Assert.SkipWhen(sassc is null, "sassc is not installed");
        var psi = new ProcessStartInfo(sassc!, ["-a", "-M", "-t", "compact", "gtk.scss"])
        {
            WorkingDirectory = Path.Combine(Root, "external/libadwaita/src/stylesheet"),
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);
        Assert.True(output == TestData.BundledCss, "the bundled stylesheet differs from sassc's output; run tools/build-stylesheet.sh");
    }

    [Fact]
    public void Package_Version_Follows_The_Pinned_Libadwaita()
    {
        var props = File.ReadAllText(Path.Combine(Root, "Directory.Build.props"));
        var meson = File.ReadAllText(Path.Combine(Root, "external/libadwaita/meson.build"));
        var libadwaita = Regex.Match(meson, @"project\('libadwaita',\s*'c',\s*version: '([0-9.]+)'").Groups[1].Value;
        Assert.Equal(libadwaita, Regex.Match(props, @"<AvaAdwaitaLibadwaitaVersion>([0-9.]+)</AvaAdwaitaLibadwaitaVersion>").Groups[1].Value);
        Assert.Equal(AvaAdwaita.Sources.StyleSheetLoader.BundledVersion, libadwaita);
        Assert.Matches(@"<Version>\$\(AvaAdwaitaLibadwaitaVersion\)\.\$\(AvaAdwaitaRevision\)</Version>", props);
    }

    [Fact]
    public void Generated_Property_Docs_Are_Current()
    {
        // The generator's check runs in CI; here the docs must at least list every property in the table.
        var docs = File.ReadAllText(Path.Combine(Root, "docs/css-support.md"));
        var missing = AvaAdwaita.CssEngine.Properties.CssPropertyTable.All.Where(p => !docs.Contains($"`{p.Name}`", StringComparison.Ordinal)).Select(p => p.Name).ToList();
        Assert.Empty(missing);
    }
}
