namespace AvaAdwaita.Gallery.Pages;

/// <summary>A row of the list and grid samples.</summary>
public sealed class FileRow
{
    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    /// <summary>The size in bytes, which the size column sorts by.</summary>
    public long Bytes { get; set; }

    public bool Starred { get; set; }
}
