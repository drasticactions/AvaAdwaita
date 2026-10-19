namespace AvaAdwaita.Controls;

/// <summary>How a header bar centers its title (AdwCenteringPolicy).</summary>
public enum CenteringPolicy
{
    /// <summary>In the middle while the sides leave room, else pushed aside by the wider side.</summary>
    Loose,

    /// <summary>Always in the middle: the sides are equally wide.</summary>
    Strict,
}
