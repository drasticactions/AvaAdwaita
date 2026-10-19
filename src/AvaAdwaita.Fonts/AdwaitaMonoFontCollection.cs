using System;
using Avalonia.Media.Fonts;

namespace AvaAdwaita.Fonts;

/// <summary>Adwaita Mono, as <c>fonts:AdwaitaMono#Adwaita Mono</c>.</summary>
public sealed class AdwaitaMonoFontCollection : EmbeddedFontCollection
{
    /// <summary>The collection's key.</summary>
    public static readonly Uri CollectionKey = new("fonts:AdwaitaMono", UriKind.Absolute);

    /// <summary>The monospace family.</summary>
    public const string Mono = "fonts:AdwaitaMono#Adwaita Mono";

    /// <summary>Creates the collection.</summary>
    public AdwaitaMonoFontCollection()
        : base(CollectionKey, new Uri("avares://AvaAdwaita.Fonts/Assets/Mono", UriKind.Absolute))
    {
    }
}
