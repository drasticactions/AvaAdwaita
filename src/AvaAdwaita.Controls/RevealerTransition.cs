namespace AvaAdwaita.Controls;

/// <summary>How a <see cref="Revealer"/> shows and hides its child (GtkRevealerTransitionType).</summary>
public enum RevealerTransition
{
    /// <summary>No animation.</summary>
    None,

    /// <summary>Fades the child in and out.</summary>
    Crossfade,

    /// <summary>Slides in from the start edge.</summary>
    SlideRight,

    /// <summary>Slides in from the end edge.</summary>
    SlideLeft,

    /// <summary>Slides in from the bottom.</summary>
    SlideUp,

    /// <summary>Slides in from the top.</summary>
    SlideDown,
}
