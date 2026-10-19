using System;

namespace AvaAdwaita.Controls;

/// <summary>A link being activated; handling it keeps the link from opening.</summary>
public class LinkActivatedEventArgs(string uri) : EventArgs
{
    /// <summary>The link's URI.</summary>
    public string Uri { get; } = uri;

    /// <summary>Whether the link was dealt with, so it does not open.</summary>
    public bool Handled { get; set; }
}
