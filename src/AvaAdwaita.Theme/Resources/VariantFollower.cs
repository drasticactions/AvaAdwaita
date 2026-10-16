using System;
using Avalonia;
using Avalonia.Styling;

namespace AvaAdwaita.Resources;

/// <summary>
/// Sets <see cref="Application.RequestedThemeVariant"/> from the resolved dark state, and treats a
/// variant the application sets itself as a forced color scheme.
/// </summary>
internal sealed class VariantFollower : IDisposable
{
    private readonly Application _app;
    private readonly Func<bool> _enabled;
    private readonly Action<Settings.StyleStateChange> _onChanged;
    private ThemeVariant? _written;

    public VariantFollower(Application app, Func<bool> enabled)
    {
        _app = app;
        _enabled = enabled;
        _onChanged = _ => Apply();
        app.PropertyChanged += OnAppPropertyChanged;
        AdwaitaEnvironment.Changed += _onChanged;
        var initial = app.RequestedThemeVariant;
        if (initial == ThemeVariant.Dark || initial == ThemeVariant.Light)
        {
            AdwaitaEnvironment.State.RequestedDark = initial == ThemeVariant.Dark;
        }

        Apply();
    }

    public Application App => _app;

    public void Dispose()
    {
        _app.PropertyChanged -= OnAppPropertyChanged;
        AdwaitaEnvironment.Changed -= _onChanged;
    }

    public void Apply()
    {
        if (!_enabled())
        {
            return;
        }

        var v = AdwaitaEnvironment.State.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        if (_app.RequestedThemeVariant != v)
        {
            _written = v;
            _app.RequestedThemeVariant = v;
        }
    }

    private void OnAppPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Application.RequestedThemeVariantProperty)
        {
            return;
        }

        var v = _app.RequestedThemeVariant;
        if (v == _written)
        {
            return;
        }

        // The application set a variant: Dark acts as ForceDark and Light as ForceLight; Default follows the system.
        AdwaitaEnvironment.State.RequestedDark = v == ThemeVariant.Dark ? true : v == ThemeVariant.Light ? false : null;
    }
}
