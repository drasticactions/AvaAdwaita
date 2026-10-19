using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A banner's title and button, laid out as adw-banner.c's measure_content and allocate_content: the button beside
/// the title while both fit, else under it; the title centered when it can be.
/// </summary>
public class BannerLayout : Panel
{
    private const double HorzSpacing = 6;
    private const double HorzSpacingCentered = 36;
    private const double VertSpacing = 9;
    private const double HorzPadding = 6;
    private const double VertPadding = 6;
    private const double ButtonHorzMinWidth = 84;
    private const double ButtonVertMinWidth = 160;

    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<Control?> TitleProperty =
        AvaloniaProperty.Register<BannerLayout, Control?>(nameof(Title));

    /// <summary>Defines <see cref="Button"/>.</summary>
    public static readonly StyledProperty<Control?> ButtonProperty =
        AvaloniaProperty.Register<BannerLayout, Control?>(nameof(Button));

    /// <summary>The title.</summary>
    public Control? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The button.</summary>
    public Control? Button
    {
        get => GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    private static Size Natural(Control c)
    {
        c.Measure(Size.Infinity);
        return c.DesiredSize;
    }

    private static double HeightFor(Control c, double width)
    {
        c.Measure(new Size(Math.Max(0, width), double.PositiveInfinity));
        return c.DesiredSize.Height;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Title is not { } title || Button is not { } button)
        {
            return default;
        }

        var buttonShown = button.IsVisible;
        var label = Natural(title);
        var buttonNat = buttonShown ? Natural(button) : default;
        var width = buttonShown
            ? Math.Max(HorzPadding + label.Width + HorzSpacing + buttonNat.Width, (HorzPadding * 2) + Math.Max(buttonNat.Width, ButtonVertMinWidth))
            : (HorzPadding * 2) + label.Width;
        var forWidth = double.IsInfinity(availableSize.Width) ? -1 : availableSize.Width;

        double height;
        if (buttonShown)
        {
            if (forWidth >= 0 && HorzPadding + label.Width + HorzSpacing + Math.Max(buttonNat.Width, ButtonHorzMinWidth) > forWidth)
            {
                height = (VertPadding * 2) + HeightFor(title, forWidth - (HorzPadding * 2)) + VertSpacing + buttonNat.Height;
            }
            else
            {
                height = Math.Max(forWidth >= 0 ? HeightFor(title, forWidth) : label.Height, buttonNat.Height);
            }
        }
        else if (forWidth >= 0 && label.Width > forWidth)
        {
            height = HeightFor(title, forWidth);
        }
        else if (forWidth >= 0 && label.Width > forWidth - (HorzPadding * 2))
        {
            height = HeightFor(title, forWidth - (HorzPadding * 2));
        }
        else
        {
            height = (VertPadding * 2) + HeightFor(title, forWidth >= 0 ? forWidth - (HorzPadding * 2) : double.PositiveInfinity);
        }

        return new Size(forWidth >= 0 ? Math.Min(width, forWidth) : width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Title is not { } title || Button is not { } button)
        {
            return finalSize;
        }

        var (width, height) = (Math.Floor(finalSize.Width), Math.Floor(finalSize.Height));
        var buttonShown = button.IsVisible;
        var labelWidth = Natural(title).Width;
        var buttonWidth = buttonShown ? Natural(button).Width : 0;
        var labelHeight = HeightFor(title, width);
        var buttonHeight = buttonShown ? HeightFor(button, width) : 0;
        labelWidth = buttonShown || labelWidth > width ? Math.Min(labelWidth, width) : Math.Min(labelWidth, width - (HorzPadding * 2));

        var labelX = Math.Floor(width / 2) - Math.Floor(labelWidth / 2);
        var labelY = Math.Floor(height / 2) - Math.Floor(labelHeight / 2);
        double buttonX = 0, buttonY = 0;
        if (buttonShown)
        {
            if (HorzPadding + labelWidth + HorzSpacing + Math.Max(buttonWidth, ButtonHorzMinWidth) > width)
            {
                // The title and the button do not fit side by side: the button goes under the title.
                labelWidth = Math.Min(labelWidth, width - (HorzPadding * 2));
                labelHeight = HeightFor(title, width - (HorzPadding * 2));
                buttonWidth = Math.Clamp(buttonWidth, ButtonVertMinWidth, width);
                labelX = Math.Floor((width - labelWidth) / 2);
                labelY = VertPadding;
                buttonX = Math.Floor(width / 2) - Math.Floor(buttonWidth / 2);
                buttonY = height - buttonHeight - VertPadding;
            }
            else
            {
                buttonWidth = Math.Max(buttonWidth, ButtonHorzMinWidth);

                // Avalonia mirrors a right-to-left banner itself.
                if (labelWidth + ((buttonWidth + HorzSpacingCentered) * 2) > width)
                {
                    labelX = HorzPadding;
                }

                buttonX = width - buttonWidth;
                buttonY = Math.Floor(height / 2) - Math.Floor(buttonHeight / 2);
            }
        }

        title.Arrange(new Rect(labelX, labelY, labelWidth, labelHeight));
        button.Arrange(new Rect(buttonX, buttonY, buttonWidth, buttonHeight));
        return finalSize;
    }
}
