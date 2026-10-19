using System;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls;

/// <summary>
/// A person's picture (AdwAvatar): <see cref="Image"/> in a circle, or else the initials of <see cref="Text"/> with
/// <see cref="ShowInitials"/>, or an icon, on a color picked from the text.
/// </summary>
public class Avatar : TemplatedControl
{
    /// <summary>libadwaita's NUMBER_OF_COLORS: the avatar.color1 to avatar.color14 classes.</summary>
    private const int ColorCount = 14;

    /// <summary>Defines <see cref="Text"/>.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<Avatar, string?>(nameof(Text));

    /// <summary>Defines <see cref="ShowInitials"/>.</summary>
    public static readonly StyledProperty<bool> ShowInitialsProperty =
        AvaloniaProperty.Register<Avatar, bool>(nameof(ShowInitials));

    /// <summary>Defines <see cref="IconName"/>.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<Avatar, string?>(nameof(IconName));

    /// <summary>Defines <see cref="Size"/>.</summary>
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<Avatar, double>(nameof(Size), 48);

    /// <summary>Defines <see cref="Image"/>.</summary>
    public static readonly StyledProperty<IImage?> ImageProperty =
        AvaloniaProperty.Register<Avatar, IImage?>(nameof(Image));

    internal static readonly DirectProperty<Avatar, string?> InitialsProperty =
        AvaloniaProperty.RegisterDirect<Avatar, string?>(nameof(Initials), a => a.Initials);

    private static readonly Random s_random = new();
    private string? _initials;
    private int _color;
    private Control? _circle;
    private TextBlock? _label;

    /// <summary>The name the avatar stands for, which gives its initials and its color.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Shows the initials of <see cref="Text"/> rather than the icon.</summary>
    public bool ShowInitials
    {
        get => GetValue(ShowInitialsProperty);
        set => SetValue(ShowInitialsProperty, value);
    }

    /// <summary>The icon without initials or an image; null is adw-avatar-default-symbolic.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>The avatar's width and height. Under 25 the initials and icon are white, for contrast.</summary>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>A picture shown in the circle instead of the initials or icon.</summary>
    public IImage? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    internal string? Initials
    {
        get => _initials;
        private set => SetAndRaise(InitialsProperty, ref _initials, value);
    }

    /// <summary>The color class number, 1 to 14.</summary>
    internal int ColorClass => _color;

    /// <summary>The initials of a name: its first letter and the first letter after its last space, in capitals.</summary>
    public static string? ExtractInitials(string? text)
    {
        var normalized = text?.ToUpper(CultureInfo.InvariantCulture).Trim().Normalize(NormalizationForm.FormC);
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        var initials = char.ConvertFromUtf32(char.ConvertToUtf32(normalized, 0));
        var space = normalized.LastIndexOf(' ');
        if (space >= 0 && space + 1 < normalized.Length)
        {
            initials += char.ConvertFromUtf32(char.ConvertToUtf32(normalized, space + 1));
        }

        return initials;
    }

    /// <summary>GLib's g_str_hash (djb2 over the UTF-8 bytes, as signed chars), which picks an avatar's color.</summary>
    internal static uint StrHash(string text)
    {
        var h = 5381u;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            h = unchecked((h << 5) + h + (uint)(sbyte)b);
        }

        return h;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_label is not null)
        {
            _label.PropertyChanged -= OnLabelPropertyChanged;
        }

        _circle = e.NameScope.Find<Control>("PART_Circle");
        _label = e.NameScope.Find<TextBlock>("PART_Initials");
        if (_label is not null)
        {
            _label.PropertyChanged += OnLabelPropertyChanged;
        }

        UpdateColor();
        UpdateContent();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            UpdateColor();
            UpdateContent();
        }
        else if (change.Property == ShowInitialsProperty || change.Property == SizeProperty || change.Property == ImageProperty)
        {
            UpdateContent();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.AvatarAutomationPeer(this);

    private void OnLabelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Css.StyleProperty)
        {
            UpdateFontSize();
        }
    }

    // set_class_color: from the text's hash, or at random without a text.
    private void UpdateColor()
    {
        _color = string.IsNullOrEmpty(Text) ? s_random.Next(1, ColorCount) : (int)(StrHash(Text) % ColorCount) + 1;
        UpdateClasses();
    }

    private void UpdateClasses()
    {
        if (_circle is null)
        {
            return;
        }

        var classes = "color" + _color.ToString(CultureInfo.InvariantCulture);
        if (Size < 25)
        {
            classes += " contrasted";
        }

        if (Image is not null)
        {
            classes += " image";
        }

        Css.SetClasses(_circle, classes);
    }

    private void UpdateContent()
    {
        Initials = Image is null && ShowInitials && !string.IsNullOrEmpty(Text) ? ExtractInitials(Text) : null;
        UpdateClasses();
        UpdateFontSize();
    }

    // update_font_size: the initials fill the largest square in the circle, less a padding that grows with the size.
    private void UpdateFontSize()
    {
        if (_label is null || Initials is not { } initials || Css.GetStyle(_label) is not { } style)
        {
            return;
        }

        var baseSize = style.Core.FontSize;
        var typeface = new Typeface(_label.FontFamily, _label.FontStyle, _label.FontWeight);
        using var layout = new TextLayout(initials, typeface, baseSize, null, lineHeight: _label.LineHeight);
        var width = Math.Ceiling(layout.WidthIncludingTrailingWhitespace);
        var height = Math.Ceiling(layout.Height);
        var square = Size / 1.4142;
        var padding = Math.Max((Size * 0.4) - 5, 0);
        var max = square - padding;
        _label.FontSize = width > 0 ? Math.Clamp(height * (max / width), 0, max) : baseSize;
    }
}
