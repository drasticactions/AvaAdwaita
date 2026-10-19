using System;
using Avalonia.Media.Fonts;

namespace AvaAdwaita.Fonts;

/// <summary>
/// Adwaita Sans, as <c>fonts:Adwaita#Adwaita Sans</c>. Adwaita Mono is a collection of its own,
/// <see cref="AdwaitaMonoFontCollection"/>, because Avalonia looks for a missing character in every family of the
/// requested one's collection first: a symbol Adwaita Sans lacks would come from Adwaita Mono rather than the
/// desktop's fallback, which never puts a monospace face in sans-serif text.
/// </summary>
public sealed class AdwaitaFontCollection : EmbeddedFontCollection
{
    /// <summary>The collection's key.</summary>
    public static readonly Uri CollectionKey = new("fonts:Adwaita", UriKind.Absolute);

    /// <summary>The interface family.</summary>
    public const string Sans = "fonts:Adwaita#Adwaita Sans";

    /// <summary>The monospace family, in <see cref="AdwaitaMonoFontCollection"/>.</summary>
    public const string Mono = AdwaitaMonoFontCollection.Mono;

    /// <summary>Creates the collection.</summary>
    public AdwaitaFontCollection()
        : base(CollectionKey, new Uri("avares://AvaAdwaita.Fonts/Assets/Sans", UriKind.Absolute))
    {
    }
}
