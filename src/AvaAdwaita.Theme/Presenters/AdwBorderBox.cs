using Avalonia.Controls;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A <see cref="Border"/> that shows its node's CSS box, for template parts a control requires to be a
/// border, as <c>ProgressBar</c>'s indicator. Its child box does the drawing.
/// </summary>
public class AdwBorderBox : Border
{
    /// <summary>Creates the border with its drawing box.</summary>
    public AdwBorderBox()
    {
        Child = new AdwBox { AppliesEffects = false };
    }
}
