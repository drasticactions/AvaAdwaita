namespace AvaAdwaita.Controls;

/// <summary>How a <see cref="LevelBar"/> shows its value (GtkLevelBarMode).</summary>
public enum LevelBarMode
{
    /// <summary>One block, filled to the value.</summary>
    Continuous,

    /// <summary>A block per whole unit between the minimum and the maximum, filled up to the value.</summary>
    Discrete,
}
