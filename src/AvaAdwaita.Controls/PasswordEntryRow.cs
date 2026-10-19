using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// An entry row for a password (AdwPasswordEntryRow): the text shows as dots, and a button after it shows and hides
/// the text.
/// </summary>
public class PasswordEntryRow : EntryRow
{
    /// <summary>Defines <see cref="RevealPassword"/>.</summary>
    public static readonly StyledProperty<bool> RevealPasswordProperty =
        TextBox.RevealPasswordProperty.AddOwner<PasswordEntryRow>();

    private readonly Button _peek = new() { Classes = { "flat" }, VerticalAlignment = VerticalAlignment.Center, Focusable = false };
    private readonly AdwIcon _peekIcon = new();

    /// <summary>Creates a row.</summary>
    public PasswordEntryRow()
    {
        _peek.Content = _peekIcon;
        _peek.Click += (_, _) => RevealPassword = !RevealPassword;
        Suffixes.Add(_peek);
        UpdatePeek();
    }

    /// <summary>Whether the password shows as text.</summary>
    public bool RevealPassword
    {
        get => GetValue(RevealPasswordProperty);
        set => SetValue(RevealPasswordProperty, value);
    }

    /// <summary>The button that shows and hides the text.</summary>
    internal Button PeekButton => _peek;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (TextBox is { } text)
        {
            // GtkPasswordEntryBuffer's dots: U+25CF.
            text.PasswordChar = '\u25cf';
            text.RevealPassword = RevealPassword;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RevealPasswordProperty)
        {
            if (TextBox is { } text)
            {
                text.RevealPassword = RevealPassword;
            }

            UpdatePeek();
        }
    }

    private void UpdatePeek()
    {
        _peekIcon.IconName = RevealPassword ? "view-conceal-symbolic" : "view-reveal-symbolic";
        _peek[!ToolTip.TipProperty] = _peek.GetResourceObservable(RevealPassword ? "AdwStringHidePassword" : "AdwStringShowPassword").ToBinding();
    }
}
