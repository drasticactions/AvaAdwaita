using System;
using System.Diagnostics;
using System.IO;
using AvaAdwaita.Sources;
using AvaAdwaita.Sources.Elf;
using AvaAdwaita.Sources.Gvdb;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class LoaderTests
{
    private static string? Gresource(string lib, string path)
    {
        try
        {
            var psi = new ProcessStartInfo("gresource", ["extract", lib, path]) { RedirectStandardOutput = true, UseShellExecute = false };
            using var p = Process.Start(psi)!;
            using var m = new MemoryStream();
            p.StandardOutput.BaseStream.CopyTo(m);
            p.WaitForExit();
            return p.ExitCode == 0 ? Convert.ToBase64String(m.ToArray()) : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    [Fact]
    public void Installed_Library_Reads_Byte_Equal_To_Gresource_Extract()
    {
        var lib = StyleSheetLoader.FindLibrary();
        Assert.SkipWhen(lib is null, "libadwaita is not installed");
        var bundle = StyleSheetLoader.ReadBundle(lib!);
        Assert.NotNull(bundle);
        foreach (var path in new[] { StyleSheetLoader.CssPath, "/org/gnome/Adwaita/styles/assets/check.svg", "/org/gnome/Adwaita/styles/assets/devel-stripes.svg" })
        {
            var expected = Gresource(lib!, path);
            Assert.SkipWhen(expected is null, "gresource is not available");
            Assert.Equal(expected, Convert.ToBase64String(bundle!.ReadResource(path)!));
        }
    }

    [Fact]
    public void Bundled_Css_Equals_Installed_1_10_0()
    {
        var lib = StyleSheetLoader.FindLibrary();
        Assert.SkipWhen(lib is null, "libadwaita is not installed");
        var installed = StyleSheetLoader.Load(AdwCssSource.Installed);
        Assert.SkipWhen(installed.Css.Length != StyleSheetLoader.Bundled().Css.Length && installed.Version != "1.10.0", "installed libadwaita is not 1.10.0");
        Assert.Equal(StyleSheetLoader.Bundled().Css, installed.Css);
        Assert.True(installed.IsKnownClean);
        Assert.Equal(4, installed.Assets.Count);
    }

    [Fact]
    public void Truncated_Library_Is_Rejected()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [0x7f, (byte)'E', (byte)'L', (byte)'F', 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3]);
            Assert.ThrowsAny<Exception>(() => ElfReader.Open(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Version_Comes_From_The_Soname_Suffix()
    {
        Assert.Equal("1.10.0", StyleSheetLoader.VersionFromPath("/usr/lib/libadwaita-1.so.0.1000.0"));
        Assert.Equal("1.7.2", StyleSheetLoader.VersionFromPath("/usr/lib/libadwaita-1.so.0.700.2"));
        Assert.Null(StyleSheetLoader.VersionFromPath("/usr/lib/libadwaita-1.so.0"));
    }

    [Fact]
    public void Bundled_Assets_Are_Embedded()
    {
        var b = StyleSheetLoader.Bundled();
        Assert.Equal(4, b.Assets.Count);
        Assert.StartsWith("<", System.Text.Encoding.UTF8.GetString(b.Assets["check.svg"]).TrimStart());
    }

    [Fact]
    public void Compressed_Resources_Are_Inflated()
    {
        // A minimal GResource with one zlib-compressed file, as glib-compile-resources writes with compressed="true".
        var content = System.Text.Encoding.UTF8.GetBytes("hello compressed world");
        var data = GvdbTestFile.Build("/a.txt", content, compressed: true);
        var reader = GvdbReader.Open(data)!;
        Assert.Equal(content, reader.ReadResource("/a.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Big_Endian_Files_Are_Read(bool compressed)
    {
        var content = System.Text.Encoding.UTF8.GetBytes("written on a big-endian machine");
        var data = GvdbTestFile.Build("/b.css", content, compressed, bigEndian: true);
        var reader = GvdbReader.Open(data)!;
        Assert.True(reader.Byteswapped);
        Assert.Equal(content, reader.ReadResource("/b.css"));
    }

    [Fact]
    public void Version_Comes_From_PkgConfig_When_The_Name_Has_None()
    {
        var dir = Directory.CreateTempSubdirectory("avaadwaita-lib");
        Directory.CreateDirectory(Path.Combine(dir.FullName, "pkgconfig"));
        File.WriteAllText(Path.Combine(dir.FullName, "pkgconfig", "libadwaita-1.pc"), "prefix=/usr\nName: libadwaita\nVersion: 1.10.0\n");
        Assert.Equal("1.10.0", StyleSheetLoader.VersionFromPkgConfig(Path.Combine(dir.FullName, "libadwaita-1.so.0")));
        Assert.Null(StyleSheetLoader.VersionFromPkgConfig("/nonexistent/libadwaita-1.so.0"));
        dir.Delete(true);
    }
}
