namespace AvaAdwaita.Controls;

/// <summary>How a <see cref="Dialog"/> shows (AdwDialogPresentationMode).</summary>
public enum DialogPresentationMode
{
    /// <summary>A bottom sheet in a window narrower than 450 or lower than 360, else floating.</summary>
    Auto,

    /// <summary>A sheet in the middle of the window.</summary>
    Floating,

    /// <summary>A sheet along the bottom of the window.</summary>
    BottomSheet,
}
