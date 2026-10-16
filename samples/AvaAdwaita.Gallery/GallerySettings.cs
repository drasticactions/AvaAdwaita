using System;
using System.IO;

namespace AvaAdwaita.Gallery;

/// <summary>The gallery's saved stylesheet choice, read at start-up because the stylesheet loads once.</summary>
public static class GallerySettings
{
    private static string? FilePath
    {
        get
        {
            if (OperatingSystem.IsBrowser())
            {
                return null;
            }

            var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(config))
            {
                config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            }

            return Path.Combine(config, "avaadwaita-gallery", "css-source");
        }
    }

    public static AdwCssSource CssSource
    {
        get
        {
            try
            {
                return FilePath is { } p && File.Exists(p) && Enum.TryParse<AdwCssSource>(File.ReadAllText(p).Trim(), out var v) ? v : AdwCssSource.Auto;
            }
            catch (IOException)
            {
                return AdwCssSource.Auto;
            }
        }

        set
        {
            if (FilePath is not { } p)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(p)!);
                File.WriteAllText(p, value.ToString());
            }
            catch (IOException)
            {
            }
        }
    }
}
