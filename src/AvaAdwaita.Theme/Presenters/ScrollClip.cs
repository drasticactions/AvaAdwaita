using Avalonia;
using Avalonia.Controls.Presenters;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Clips a scroll presenter only while its content scrolls. GTK lists do not clip their rows, so a row's
/// shadow reaches past the list; a list that fits its rows keeps them unclipped.
/// </summary>
public static class ScrollClip
{
    /// <summary>True to clip only while the content is larger than the viewport.</summary>
    public static readonly AttachedProperty<bool> AutoProperty =
        AvaloniaProperty.RegisterAttached<ScrollContentPresenter, bool>("Auto", typeof(ScrollClip));

    static ScrollClip()
    {
        AutoProperty.Changed.AddClassHandler<ScrollContentPresenter>((p, _) => Update(p));
        ScrollContentPresenter.ExtentProperty.Changed.AddClassHandler<ScrollContentPresenter>((p, _) => Update(p));
        ScrollContentPresenter.ViewportProperty.Changed.AddClassHandler<ScrollContentPresenter>((p, _) => Update(p));
    }

    /// <summary>Gets <see cref="AutoProperty"/>.</summary>
    public static bool GetAuto(ScrollContentPresenter p) => p.GetValue(AutoProperty);

    /// <summary>Sets <see cref="AutoProperty"/>.</summary>
    public static void SetAuto(ScrollContentPresenter p, bool v) => p.SetValue(AutoProperty, v);

    private static void Update(ScrollContentPresenter p)
    {
        if (!p.GetValue(AutoProperty))
        {
            return;
        }

        var e = p.Extent;
        var v = p.Viewport;
        p.ClipToBounds = e.Width > v.Width + 0.5 || e.Height > v.Height + 0.5;
    }
}
