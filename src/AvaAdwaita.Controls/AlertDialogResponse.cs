using Avalonia;

namespace AvaAdwaita.Controls;

/// <summary>A response an <see cref="AlertDialog"/> offers: a button with a label and an appearance.</summary>
public class AlertDialogResponse : AvaloniaObject
{
    /// <summary>Defines <see cref="Id"/>.</summary>
    public static readonly StyledProperty<string> IdProperty =
        AvaloniaProperty.Register<AlertDialogResponse, string>(nameof(Id), string.Empty);

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AlertDialogResponse, string?>(nameof(Label));

    /// <summary>Defines <see cref="Appearance"/>.</summary>
    public static readonly StyledProperty<ResponseAppearance> AppearanceProperty =
        AvaloniaProperty.Register<AlertDialogResponse, ResponseAppearance>(nameof(Appearance));

    /// <summary>Defines <see cref="IsEnabled"/>.</summary>
    public static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<AlertDialogResponse, bool>(nameof(IsEnabled), true);

    /// <summary>Creates a response.</summary>
    public AlertDialogResponse()
    {
    }

    /// <summary>Creates a response with an id and a label.</summary>
    public AlertDialogResponse(string id, string label, ResponseAppearance appearance = ResponseAppearance.Default)
    {
        Id = id;
        Label = label;
        Appearance = appearance;
    }

    /// <summary>The id <see cref="AlertDialog.ChooseAsync"/> returns for the response.</summary>
    public string Id
    {
        get => GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    /// <summary>The button's label, with <c>_</c> before its access key.</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>How the button looks.</summary>
    public ResponseAppearance Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    /// <summary>Whether the button can be clicked.</summary>
    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }
}
