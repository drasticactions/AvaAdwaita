namespace AvaAdwaita.CssEngine.Values;

/// <summary>One <c>font-feature-settings</c> or <c>font-variation-settings</c> entry.</summary>
internal readonly record struct CssFontSetting(string Tag, double Value);
