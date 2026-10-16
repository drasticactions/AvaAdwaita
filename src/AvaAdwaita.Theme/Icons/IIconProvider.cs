namespace AvaAdwaita.Icons;

/// <summary>Finds icons by name.</summary>
public interface IIconProvider
{
    /// <summary>Looks up an icon for a pixel size, or returns null.</summary>
    /// <param name="name">The icon name, as <c>pan-down-symbolic</c>.</param>
    /// <param name="size">The size in pixels.</param>
    /// <param name="rtl">True to prefer the <c>-rtl</c> variant.</param>
    IconData? Lookup(string name, int size, bool rtl);
}
