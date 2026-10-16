namespace AvaAdwaita;

/// <summary>The application's color scheme preference, as <c>AdwColorScheme</c>.</summary>
public enum AdwColorScheme
{
    /// <summary>Follow the system; light when the system has no preference.</summary>
    Default,

    /// <summary>Always light.</summary>
    ForceLight,

    /// <summary>Light, unless the system prefers dark.</summary>
    PreferLight,

    /// <summary>Dark, unless the system prefers light.</summary>
    PreferDark,

    /// <summary>Always dark.</summary>
    ForceDark,
}
