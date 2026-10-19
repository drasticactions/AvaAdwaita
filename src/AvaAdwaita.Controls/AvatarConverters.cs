using Avalonia.Data.Converters;

namespace AvaAdwaita.Controls;

/// <summary>Converters for <see cref="Avatar"/>'s template.</summary>
public static class AvatarConverters
{
    /// <summary>The icon name, or libadwaita's default avatar icon.</summary>
    public static readonly IValueConverter IconName = new FuncValueConverter<string?, string>(n => string.IsNullOrEmpty(n) ? "adw-avatar-default-symbolic" : n);

    /// <summary>Half the avatar's size: the icon's pixel size.</summary>
    public static readonly IValueConverter Half = new FuncValueConverter<double, double>(s => s / 2);
}
