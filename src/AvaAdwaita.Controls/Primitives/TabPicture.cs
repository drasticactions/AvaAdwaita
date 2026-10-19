using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaAdwaita.Nodes;
using AvaAdwaita.Icons;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab thumbnail's picture of its page (a <c>picture</c> node showing AdwTabPaintable): the page as it last showed,
/// stretched over the picture, which is as tall as the view's shape makes it for its width.
/// </summary>
public class TabPicture : AdwBox
{
    private const double MinAspectRatio = 0.8;
    private const double MaxAspectRatio = 2.7;

    /// <summary>Creates a picture of <paramref name="view"/>'s pages.</summary>
    public TabPicture(TabView view)
    {
        View = view;
        Css.SetNode(this, "picture");

        // A picture drawn for another width scales filtered with mipmaps rather than sampled, so that its text still reads.
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    /// <summary>The view whose shape the picture takes.</summary>
    public TabView View { get; }

    /// <summary>The page's last image.</summary>
    public IImage? Source
    {
        get => _source;
        set
        {
            _source = value;
            InvalidateVisual();
        }
    }

    private IImage? _source;
    private Color _windowBg = Colors.White;
    private Color _thumbnailBg = Colors.White;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        View.PropertyChanged += OnViewPropertyChanged;
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        View.PropertyChanged -= OnViewPropertyChanged;
    }

    // The picture takes the view's shape as it changes.
    private void OnViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty && ((Rect)e.OldValue!).Size != ((Rect)e.NewValue!).Size)
        {
            InvalidateMeasure();
        }
    }

    /// <summary>adw_tab_paintable_get_intrinsic_aspect_ratio: the view's, clamped.</summary>
    internal double AspectRatio => View.Bounds.Height > 0 ? Math.Clamp(View.Bounds.Width / View.Bounds.Height, MinAspectRatio, MaxAspectRatio) : 1;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // GtkPicture with can-shrink: no width of its own, and a height for its width.
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        return new Size(0, Math.Ceiling(width / AspectRatio));
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        // The picture under the node's outline: the page on the window's color, or, for a page that has not shown yet,
        // the view's default icon on the thumbnail color (adw_tab_paintable_snapshot).
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            // Within the card's rounded corners, which clip it.
            var clip = CurrentStyle is { } s ? new CssBoxGeometry(s, bounds.Size).PaddingRounded : new RoundedRect(bounds);
            using (context.PushClip(clip))
            {
                if (_source is { } source)
                {
                    Fill(context, clip, false);

                    context.DrawImage(source, new Rect(source.Size), bounds);
                }
                else
                {
                    Fill(context, clip, true);
                    DrawDefaultIcon(context, bounds);
                }
            }
        }

        base.Render(context);
    }

    private void Fill(DrawingContext context, RoundedRect clip, bool empty)
    {
        var color = empty ? _thumbnailBg : _windowBg;
        context.FillRectangle(new SolidColorBrush(color), clip.Rect);
        DrawRecorder.For(this)?.Add(this, new DrawRecord("color", clip.Rect) { Color = color, Radii = DrawRecorder.Radii(clip) });
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // The colors come with the style, which follows the theme; looking them up while drawing would restyle then.
        if (change.Property == Css.StyleProperty)
        {
            _windowBg = this.TryFindResource("Adw.WindowBgColor", out var w) && w is Color wc ? wc : Colors.White;
            _thumbnailBg = this.TryFindResource("Adw.ThumbnailBgColor", out var t) && t is Color tc ? tc : Colors.White;
        }
    }

    // snapshot_default_icon: the icon a quarter of the view's size, in the middle of the view's shape scaled into the
    // picture, faint.
    private void DrawDefaultIcon(DrawingContext context, Rect bounds)
    {
        var (viewWidth, viewHeight) = (View.Bounds.Width, View.Bounds.Height);
        if (viewWidth <= 0 || viewHeight <= 0 || Css.GetStyle(View) is not { } style)
        {
            return;
        }

        var (width, height) = (bounds.Width, bounds.Height);
        var (x0, y0) = (0.0, 0.0);
        var viewRatio = viewWidth / viewHeight;
        if (viewRatio > width / height)
        {
            var newWidth = height * viewRatio;
            x0 = (width - newWidth) / 2;
            width = newWidth;
        }
        else if (viewRatio < width / height)
        {
            var newHeight = width / viewRatio;
            y0 = (height - newHeight) / 2;
            height = newHeight;
        }

        var iconSize = Math.Min(viewWidth / 4, viewHeight / 4);
        if (IconProviders.Lookup(View.DefaultIconName, (int)Math.Ceiling(iconSize), FlowDirection == FlowDirection.RightToLeft) is not { } icon)
        {
            return;
        }

        var (sx, sy) = (width / viewWidth, height / viewHeight);
        var box = new Rect(x0 + ((viewWidth - iconSize) / 2 * sx), y0 + ((viewHeight - iconSize) / 2 * sy), iconSize * sx, iconSize * sy);
        var hc = AdwaitaEnvironment.State.HighContrast;
        using (context.PushOpacity(hc ? 0.3 : 0.15))
        {
            AdwIcon.Paint(context, icon, box, style, null);
        }
    }
}
