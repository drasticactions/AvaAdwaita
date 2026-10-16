using System;
using AvaAdwaita.Settings;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Settings that never change, for a test application that must not depend on the desktop.</summary>
internal sealed class FixedSettings(SettingsValues values) : ISettingsSource
{
    public string Name => "fixed";

    public SettingsValues Current { get; } = values;

    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    public void Dispose()
    {
    }
}
