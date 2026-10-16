using System;

namespace AvaAdwaita.Settings;

/// <summary>A source of desktop settings: the portal, dconf, the platform, or the debug environment.</summary>
public interface ISettingsSource : IDisposable
{
    /// <summary>A name for diagnostics: "portal", "dconf", "platform", "environment".</summary>
    string Name { get; }

    /// <summary>The values found so far.</summary>
    SettingsValues Current { get; }

    /// <summary>Raised, on any thread, when <see cref="Current"/> changes.</summary>
    event EventHandler? Changed;
}
