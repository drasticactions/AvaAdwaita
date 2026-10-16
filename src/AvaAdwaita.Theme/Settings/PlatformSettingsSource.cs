using System;
using Avalonia.Platform;

namespace AvaAdwaita.Settings;

/// <summary>Reads light or dark, the accent color and contrast from Avalonia's <see cref="IPlatformSettings"/>.</summary>
/// <remarks>The settings source on Windows, macOS and the browser.</remarks>
public sealed class PlatformSettingsSource : ISettingsSource
{
    private readonly IPlatformSettings _settings;
    private SettingsValues _current;

    /// <summary>Follows <paramref name="settings"/>.</summary>
    public PlatformSettingsSource(IPlatformSettings settings)
    {
        _settings = settings;
        _current = Build(settings.GetColorValues());
        settings.ColorValuesChanged += OnChanged;
    }

    /// <inheritdoc/>
    public string Name => "platform";

    /// <inheritdoc/>
    public SettingsValues Current => _current;

    /// <inheritdoc/>
    public event EventHandler? Changed;

    private void OnChanged(object? sender, PlatformColorValues e)
    {
        var next = Build(e);
        if (next != _current)
        {
            _current = next;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static SettingsValues Build(PlatformColorValues v)
    {
        var a = v.AccentColor1;
        (double, double, double)? accent = a.A == 0 ? null : (a.R / 255.0, a.G / 255.0, a.B / 255.0);
        string? layout = OperatingSystem.IsMacOS() ? "close,minimize,maximize:" : null;
        return new SettingsValues
        {
            ColorScheme = v.ThemeVariant == PlatformThemeVariant.Dark ? SystemColorScheme.PreferDark : SystemColorScheme.Default,
            AccentRgb = accent,
            HighContrast = v.ContrastPreference == ColorContrastPreference.High,
            ButtonLayout = layout,
        };
    }

    /// <inheritdoc/>
    public void Dispose() => _settings.ColorValuesChanged -= OnChanged;
}
