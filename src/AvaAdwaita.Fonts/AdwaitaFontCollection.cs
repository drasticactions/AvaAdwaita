using System;
using Avalonia.Media.Fonts;

namespace AvaAdwaita.Fonts;

/// <summary>Adwaita Sans and Adwaita Mono, as <c>fonts:Adwaita#Adwaita Sans</c> and <c>fonts:Adwaita#Adwaita Mono</c>.</summary>
public sealed class AdwaitaFontCollection : EmbeddedFontCollection
{
    /// <summary>The collection's key.</summary>
    public static readonly Uri CollectionKey = new("fonts:Adwaita", UriKind.Absolute);

    /// <summary>The interface family.</summary>
    public const string Sans = "fonts:Adwaita#Adwaita Sans";

    /// <summary>The monospace family.</summary>
    public const string Mono = "fonts:Adwaita#Adwaita Mono";

    /// <summary>Creates the collection.</summary>
    public AdwaitaFontCollection()
        : base(CollectionKey, new Uri("avares://AvaAdwaita.Fonts/Assets", UriKind.Absolute))
    {
    }
}
