namespace AvaAdwaita;

/// <summary>Which libadwaita stylesheet the theme runs.</summary>
public enum AdwCssSource
{
    /// <summary>The installed libadwaita's stylesheet when it parses cleanly, else the bundled 1.10.0 copy.</summary>
    Auto,

    /// <summary>The installed stylesheet; loading throws when it cannot be used. For tests.</summary>
    Installed,

    /// <summary>Always the bundled libadwaita 1.10.0 stylesheet.</summary>
    Bundled,
}
