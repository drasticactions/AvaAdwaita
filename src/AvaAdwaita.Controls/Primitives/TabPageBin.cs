using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>A tab page's bin in its view (AdwTabPage's bin), which notes whether it has ever drawn, for its thumbnail.</summary>
internal sealed class TabPageBin : AdwBox
{
    /// <summary>Whether the page has drawn: until then its thumbnail shows the view's default icon.</summary>
    public bool HasRendered { get; private set; }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        // Avalonia records hidden visuals' drawing too: the page has shown when it and its ancestors show.
        if (!HasRendered && IsEffectivelyVisible && Shows(this))
        {
            HasRendered = true;
        }

        base.Render(context);
    }

    private static bool Shows(Visual? v)
    {
        for (; v is not null; v = v.GetVisualParent())
        {
            if (v.Opacity <= 0)
            {
                return false;
            }
        }

        return true;
    }
}
