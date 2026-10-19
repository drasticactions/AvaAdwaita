using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.Nodes;
using AvaAdwaita.Resources;

namespace AvaAdwaita;

/// <summary>
/// Makes the application look like a libadwaita application. Add it to <c>Application.Styles</c>.
/// It runs libadwaita's stylesheet and follows the desktop's color scheme, accent, contrast and fonts.
/// </summary>
public class AdwaitaTheme : Styles
{
    private readonly AdwResourceProvider _provider = new();
    private VariantFollower? _follower;

    /// <summary>Creates the theme.</summary>
    public AdwaitaTheme()
    {
        Nodes.Css.EnsureRegistered();
        ControlHints.Register();
        AdwaitaEnvironment.Preload();
        Resources.MergedDictionaries.Add(_provider);
        Resources.MergedDictionaries.Add(new Themes.ControlThemes());

        // After the theme's English strings, so their translations take precedence.
        Resources.MergedDictionaries.Add(AvaAdwaita.Resources.GtkStrings.For(System.Globalization.CultureInfo.CurrentUICulture));

        // GTK does not clip a widget's drawing to its allocation: shadows and negative margins reach outside.
        Add(new Style(x => x.Is<Avalonia.Controls.Primitives.TemplatedControl>())
        {
            Setters = { new Setter(Visual.ClipToBoundsProperty, false) },
        });

        // A header bar's windowhandle moves the window from a press anywhere a widget does not take it: a button or
        // entry in the header bar gets the press, as in GTK, rather than the title bar role of the bar around it.
        Add(new Style(x => x.Is<Avalonia.Controls.Primitives.TemplatedControl>().PropertyEquals(Avalonia.Input.InputElement.FocusableProperty, true))
        {
            Setters =
            {
                new Setter(Avalonia.Controls.Chrome.WindowDecorationProperties.ElementRoleProperty, Avalonia.Input.WindowDecorationsElementRole.DecorationsElement),
            },
        });

        // A button takes its press even when it takes no focus, as a dialog's close button.
        Add(new Style(x => x.Is<Avalonia.Controls.Button>())
        {
            Setters =
            {
                new Setter(Avalonia.Controls.Chrome.WindowDecorationProperties.ElementRoleProperty, Avalonia.Input.WindowDecorationsElementRole.DecorationsElement),
            },
        });
        _provider.OwnerChanged += (_, _) => Attach();
        StyleSheets.CollectionChanged += (_, _) => LoadStyleSheets();
    }

    /// <summary>The color scheme, as <c>AdwStyleManager:color-scheme</c>.</summary>
    public AdwColorScheme ColorScheme
    {
        get => AdwaitaEnvironment.State.ColorScheme;
        set => AdwaitaEnvironment.State.ColorScheme = value;
    }

    /// <summary>The accent color; <see cref="AdwAccentColor.System"/> follows the desktop.</summary>
    public AdwAccentColor AccentColor
    {
        get => AdwaitaEnvironment.State.AccentOverride;
        set => AdwaitaEnvironment.State.AccentOverride = value;
    }

    /// <summary>High contrast.</summary>
    public AdwHighContrast HighContrast
    {
        get => AdwaitaEnvironment.State.HighContrastOverride;
        set => AdwaitaEnvironment.State.HighContrastOverride = value;
    }

    /// <summary>Which stylesheet to run. Only takes effect before the theme is first used.</summary>
    public AdwCssSource CssSource
    {
        get => AdwaitaEnvironment.Options.CssSource;
        set => AdwaitaEnvironment.Options.CssSource = value;
    }

    /// <summary>Application style sheets, as <c>avares://</c> or file URIs, loaded at application priority in order.</summary>
    public ObservableCollection<string> StyleSheets { get; } = new();

    /// <summary>Set <see cref="Application.RequestedThemeVariant"/> from the resolved dark state.</summary>
    public bool FollowThemeVariant { get; set; } = true;

    /// <summary>The stylesheet in use: "installed 1.10.0" or "bundled 1.10.0".</summary>
    public string StyleSheetDescription
    {
        get
        {
            AdwaitaEnvironment.EnsureInitialized();
            return AdwaitaEnvironment.Source.Description;
        }
    }

    /// <summary>The resolved style state.</summary>
    public Settings.StyleState State => AdwaitaEnvironment.State;

    /// <summary>The style engine, for diagnostics.</summary>
    public CssStyleEngine Engine => AdwaitaEnvironment.Engine;

    private void Attach()
    {
        // The owner is the application when the theme is in its styles, even before Application.Current is set.
        if ((Owner as Application ?? Application.Current) is not { } app || ReferenceEquals(_follower?.App, app))
        {
            return;
        }

        AdwaitaEnvironment.EnsureInitialized();
        _follower?.Dispose();
        _follower = new VariantFollower(app, () => FollowThemeVariant);
        LoadStyleSheets();
    }

    private void LoadStyleSheets()
    {
        if (!AdwaitaEnvironment.IsInitialized)
        {
            return;
        }

        var sheets = new List<CssStyleSheet>();
        foreach (var uri in StyleSheets)
        {
            if (Load(uri) is { } text)
            {
                var sheet = CssStyleSheet.Parse(text, CssOrigin.Application, uri, BaseOf(uri), u => Load(u));
                foreach (var e in sheet.Errors)
                {
                    if (e.Kind != CssEngine.Syntax.CssErrorKind.Warning)
                    {
                        Console.Error.WriteLine($"AvaAdwaita: {uri}:{e}");
                    }
                }

                sheets.Add(sheet);
            }
        }

        AdwaitaEnvironment.SetAppStyleSheets(sheets);
    }

    private static string BaseOf(string uri)
    {
        var i = uri.LastIndexOf('/');
        return i >= 0 ? uri[..(i + 1)] : uri;
    }

    private static string? Load(string uri)
    {
        try
        {
            if (uri.StartsWith("avares://", StringComparison.Ordinal))
            {
                using var s = AssetLoader.Open(new Uri(uri));
                using var r = new StreamReader(s);
                return r.ReadToEnd();
            }

            var path = uri.StartsWith("file://", StringComparison.Ordinal) ? new Uri(uri).LocalPath : uri;
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception e) when (e is IOException or UriFormatException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"AvaAdwaita: could not load {uri}: {e.Message}");
            return null;
        }
    }
}
