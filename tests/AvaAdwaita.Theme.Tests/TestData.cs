using System;
using System.IO;
using System.Reflection;

namespace AvaAdwaita.Theme.Tests;

internal static class TestData
{
    public static string RepoRoot { get; } = FindRoot();

    public static string BundledCssPath => Path.Combine(RepoRoot, "src/AvaAdwaita.Theme/Sources/Bundled/gtk-1.10.0.css");

    public static string BundledCss => File.ReadAllText(BundledCssPath);

    private static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "AvaAdwaita.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir ?? throw new InvalidOperationException("repository root not found");
    }
}
