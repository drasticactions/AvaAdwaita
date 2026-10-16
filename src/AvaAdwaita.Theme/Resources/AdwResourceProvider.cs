using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;
using AvaAdwaita.Markup;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Resources;

/// <summary>
/// Serves values computed on the <c>window</c> node: every custom property (<c>Adw.AccentBgColor</c>,
/// <c>Adw.AccentBgColorBrush</c>, <c>Adw.WindowRadius</c>), every named color (<c>Adw.Named.accent_color</c>),
/// the fonts, the selection brush (<c>AdwSelectionBrush</c>), and a short list of Fluent keys so third-party
/// controls written against Fluent follow.
/// </summary>
public sealed class AdwResourceProvider : ResourceProvider
{
    private readonly Dictionary<bool, Dictionary<string, object>> _values = new();

    /// <summary>Creates the provider and follows the style state.</summary>
    public AdwResourceProvider()
    {
        AdwaitaEnvironment.Changed += _ =>
        {
            _values.Clear();
            RaiseResourcesChanged();
        };
    }

    /// <inheritdoc/>
    public override bool HasResources => true;

    /// <inheritdoc/>
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        value = null;
        if (key is not string k)
        {
            return false;
        }

        if (!AdwaitaEnvironment.IsInitialized)
        {
            AdwaitaEnvironment.EnsureInitialized();
        }

        var dark = theme == ThemeVariant.Dark || (theme != ThemeVariant.Light && AdwaitaEnvironment.State.Dark);
        if (!_values.TryGetValue(dark, out var table))
        {
            table = Compute(dark);
            _values[dark] = table;
        }

        return table.TryGetValue(k, out value);
    }

    /// <summary>Every key the provider serves for the current state, for the gallery's color page.</summary>
    public IReadOnlyCollection<string> Keys
    {
        get
        {
            TryGetResource("Adw.WindowBgColor", null, out _);
            return _values.TryGetValue(AdwaitaEnvironment.State.Dark, out var t) ? t.Keys : [];
        }
    }

    private static Dictionary<string, object> Compute(bool dark)
    {
        var state = AdwaitaEnvironment.State;
        var media = state.Media with { Dark = dark };
        var engine = AdwaitaEnvironment.Engine;
        var probe = new CssNode("window") { MediaOverride = media, States = CssStates.DirLtr };
        probe.SetClasses(["background", "csd"]);
        engine.AddRoot(probe);
        ComputedStyle style;
        try
        {
            style = engine.Resolve(probe);
        }
        finally
        {
            engine.RemoveRoot(probe);
        }

        var scope = engine.ResolverFor(media).Styles;
        var table = new Dictionary<string, object>(StringComparer.Ordinal);
        var current = style.Core.Color;
        foreach (var name in style.Custom.Names)
        {
            if (!style.Custom.TryGet(name, out var raw))
            {
                continue;
            }

            var key = "Adw." + Pascal(name);
            var r = new CssReader(raw);
            if (CssColorParser.CanParse(r) && CssColorParser.Parse(r) is { } expr && r.AtEnd && expr.Resolve(scope, current) is { } c)
            {
                table[key] = CssConvert.Color(c);
                table[key + "Brush"] = CssConvert.Brush(c);
                continue;
            }

            r = new CssReader(raw);
            if (CssNumeric.CanParse(r) && CssNumeric.Parse(r, CssNumberFlags.Length | CssNumberFlags.Number | CssNumberFlags.Percent) is { } n && r.AtEnd)
            {
                var v = n.Compute(new CssNumericContext(style.Core.FontSize, engine.FontSizePx, 96));
                table[key] = v.Class == CssDimensionClass.Percent ? v.Percent / 100 : v.Value;
            }
        }

        foreach (var name in scope.ColorNames)
        {
            if (scope.LookupColor(name)?.Resolve(scope, current) is { } c)
            {
                table["Adw.Named." + name] = CssConvert.Color(c);
                table["Adw.Named." + name + "Brush"] = CssConvert.Brush(c);
            }
        }

        var fontFamily = CssConvert.FontFamily(style.Font.Family);
        table["AdwFontFamily"] = fontFamily;
        table["AdwFontSize"] = style.Core.FontSize;
        table["AdwDocumentFontFamily"] = CssConvert.FontFamily([state.DocumentFont.Family ?? "Sans"]);
        table["AdwDocumentFontSize"] = state.DocumentFont.SizePx ?? style.Core.FontSize;
        table["AdwMonospaceFontFamily"] = CssConvert.FontFamily([state.MonospaceFont.Family ?? "Monospace"]);
        table["AdwMonospaceFontSize"] = state.MonospaceFont.SizePx ?? style.Core.FontSize;

        // Fluent compatibility keys.
        table["ContentControlThemeFontFamily"] = fontFamily;
        table["ControlContentThemeFontSize"] = style.Core.FontSize;
        if (table.TryGetValue("Adw.AccentBgColor", out var accent) && accent is Color ac)
        {
            foreach (var k in new[] { "SystemAccentColor", "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3", "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3" })
            {
                table[k] = ac;
            }

            table["SystemControlHighlightAccentBrush"] = table["Adw.AccentBgColorBrush"];

            // selection:focus-within mixes the accent background 30 % with transparent.
            table["AdwSelectionBrush"] = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromArgb((byte)Math.Round(ac.A * 0.3), ac.R, ac.G, ac.B));
        }

        if (table.TryGetValue("Adw.WindowFgColorBrush", out var fg))
        {
            table["TextFillColorPrimaryBrush"] = fg;
            table["SystemControlForegroundBaseHighBrush"] = fg;
        }

        if (table.TryGetValue("Adw.WindowBgColorBrush", out var bg))
        {
            table["SystemControlBackgroundAltHighBrush"] = bg;
            table["SystemRegionBrush"] = bg;
        }

        return table;
    }

    /// <summary><c>--accent-bg-color</c> → <c>AccentBgColor</c>.</summary>
    internal static string Pascal(string custom)
    {
        var sb = new StringBuilder();
        var upper = true;
        foreach (var ch in custom.TrimStart('-'))
        {
            if (ch == '-')
            {
                upper = true;
                continue;
            }

            sb.Append(upper ? char.ToUpperInvariant(ch) : ch);
            upper = false;
        }

        return sb.ToString();
    }
}
